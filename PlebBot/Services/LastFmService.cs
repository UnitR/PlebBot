using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlebBot.Services.Chart;
using PlebBot.TypeReaders;

namespace PlebBot.Services
{
    public class LastFmService
    {
        private readonly string lastFmKey;
        private readonly HttpClient httpClient;
        private const string NotFound = "last.fm user not found.";
        private readonly EmbedBuilder errorEmbed = new EmbedBuilder().WithTitle("Error").WithColor(Color.DarkRed);

        public LastFmService(HttpClient client)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            lastFmKey = config["tokens:lastfm_key"];
            httpClient = client;
        }

        public async Task<dynamic> GetTopAsync(ListType chart, int limit, string span = "", string username = "")
        {
            if (!await CheckIfUserExistsAsync(username))
                return errorEmbed.WithDescription(NotFound);

            if (limit < 1 || limit > 25)
                return errorEmbed.WithDescription(
                    "Check the given limit and try again. Must be a number between 1 and 25.");

            dynamic response;
            span = await DetermineSpan(span);
            switch (chart)
            {
                case ListType.Artists:
                    response = await GetTopArtistsAsync(username, span, limit);
                    break;
                case ListType.Albums:
                    response = await GetTopAlbumsAsync(username, span, limit);
                    break;
                case ListType.Tracks:
                    response = await GetTopTracksAsync(username, span, limit);
                    break;
                default:
                    return null;
            }
            return response;
        }

        private async Task<dynamic> GetLastFmData(string call)
        {
            var json = await httpClient.GetStringAsync(call);
            dynamic response = JsonConvert.DeserializeObject(json);
            return response;
        }

        //show user's scrobbles
        public async Task<EmbedBuilder> NowPlayingAsync(string username)
        {
            if (!await CheckIfUserExistsAsync(username))
                return errorEmbed.WithDescription(NotFound);

            var call =
                $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}" +
                $"&api_key={lastFmKey}&page=1&limit=2&format=json";
            var response = await GetLastFmData(call);
            if (response.recenttracks.track.Count <= 0)
                return errorEmbed.WithDescription("The user hasn't scrobbled any tracks.");

            var tracks = response.recenttracks.track;
            string currAlbum = tracks[0].album["#text"] ?? "";
            string prevAlbum = tracks[1].album["#text"] ?? "";
            var albumArt = await ChooseImage(tracks[0].image);
            
            var embed = new EmbedBuilder();
            var currField = $"{tracks[0].artist["#text"]} - {tracks[0].name}";
            var prevField = $"{tracks[1].artist["#text"]} - {tracks[1].name}";
            if (currAlbum.Length > 0) currField += $" [{currAlbum}]";
            if (prevAlbum.Length > 0) prevField += $" [{prevAlbum}]";
            embed.WithTitle($"Recent tracks for {username}")
                .WithThumbnailUrl(albumArt)
                .WithUrl($"https://www.last.fm/user/{username}")
                .AddField("**Current:**", currField)
                .AddField("**Previous:**", prevField)
                .WithColor(Color.DarkBlue);

            return embed;
        }

        public static Task<string> ChooseImage(dynamic imageArray)
        {
            var imageUrl = "";
            for (var i = 3; i >= 0; i++)
            {
                if (imageArray[i] == null) continue;
                imageUrl = imageArray[i]["#text"];
                break;
            }

            return Task.FromResult(imageUrl);
        }

        public async Task<string> GetVideoLinkAsync(string username)
        {
            var call =
                $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}" +
                $"&api_key={lastFmKey}&page=1&limit=1&format=json";
            var response = await GetLastFmData(call);
            if (response.recenttracks.track.Count <= 0) return null;
            var track = response.recenttracks.track[0];
            var ytService = new YtService();
            var link = await ytService.GetVideoLinkAsync($"{track.artist["#text"]} {track.name}");
            return link;
        }

        private async Task<dynamic> GetTopAlbumsAsync(string username, string span, int limit)
        {
            var call = $"http://ws.audioscrobbler.com/2.0/?method=user.gettopalbums&user={username}" +
                       $"&api_key={lastFmKey}&period={span}&limit={limit}&format=json";
            var response = await GetLastFmData(call);
            return response.topalbums.album.Count <= 0 ? null : response;
        }

        private async Task<dynamic> GetTopArtistsAsync(string username, string span, int limit)
        {
            var call = $"http://ws.audioscrobbler.com/2.0/?method=user.gettopartists&user={username}" +
                       $"&api_key={lastFmKey}&period={span}&limit={limit}&format=json";
            var response = await GetLastFmData(call);
            return response.topartists.artist.Count <= 0 ? null : response;
        }

        private async Task<dynamic> GetTopTracksAsync(string username, string span, int limit)
        {
            var call =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={lastFmKey}&format=json";
            var response = await GetLastFmData(call);
            return response.toptracks.track.Count <= 0 ? null : response;
        }

        //determines the time span used for the chart
        private static Task<string> DetermineSpan(string span)
        {
            switch (span.ToLower())
            {
                case "week":
                case "7days":
                case "7day":
                    span = ChartSpan.Week;
                    break;
                case "month":
                case "30day":
                case "30days":
                    span = ChartSpan.Month;
                    break;
                case "3months":
                case "3month":
                case "90days":
                case "90day":
                    span = ChartSpan.Quarter;
                    break;
                case "6months":
                case "6month":
                    span = ChartSpan.Half;
                    break;
                case "year":
                case "12month":
                case "12months":
                    span = ChartSpan.Year;
                    break;
                default:
                    span = ChartSpan.Overall;
                    break;
            }
            return Task.FromResult(span);
        }

        public static Task<string> FormatSpan(string span)
        {
            switch (span)
            {
                case "1month":
                case "month":
                    span = "month";
                    break;
                case "3month":
                case "6month":
                    span = $"{span.First()} {span.Substring(1)}s";
                    break;
                case "3months":
                case "6months":
                    span = $"{span.First()} {span.Substring(1)}";
                    break;
                case "7day":
                case "week":
                    span = "week";
                    break;
                case "7days":
                    span = $"{span.First()} {span.Substring(1)}";
                    break;
                case "12month":
                case "12months":
                    span = "year";
                    break;
                default:
                    span = "overall";
                    break;
            }
            return Task.FromResult(new CultureInfo("en-US").TextInfo.ToTitleCase(span));
        }

        //Checks if the last.fm user exists
        private async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var call = $"http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user={username}" +
                       $"&api_key={lastFmKey}&format=json";
            var response = await GetLastFmData(call);

            return response.user != null;
        }

        public async Task<string> TotalScrobblesAsync(string span, string username)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long offset = 0;
            switch (span)
            {
                case "7day":
                case "week":
                    offset = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
                    break;
                case "1month":
                case "month":
                    offset = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
                    break;
                case "3month":
                case "3months":
                case "quarter":
                    offset = DateTimeOffset.UtcNow.AddDays(-91).ToUnixTimeSeconds();
                    break;
                case "6month":
                case "6months":
                case "half":
                    offset = DateTimeOffset.UtcNow.AddDays(-182).ToUnixTimeSeconds();
                    break;
                case "12month":
                case "12months":
                case "year":
                    offset = DateTimeOffset.UtcNow.AddDays(-365).ToUnixTimeSeconds();
                    break;
            }

            var call = "http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" +
                        $"{username}&from={offset}&to={now}&api_key={lastFmKey}&format=json";
            var response = await GetLastFmData(call);
            int scrobbles = response.recenttracks["@attr"].total;

            return $"[{String.Format("{0:n0}", scrobbles)} scrobbles total]";
        }

        public async Task<dynamic> GetInformationAsync(Category category, string query)
        {
            dynamic response;
            switch (category)
            {
                case Category.Artist:
                    response = await GetArtistInfoAsync(query);
                    break;
                case Category.Album:
                    if (query.Contains(" - ")) response = await GetAlbumInfoAsync(query);
                    else
                    {
                        var searchResult = await SearchLastFm(category, query);
                        response = await GetAlbumInfoAsync($"{searchResult.artist} - {searchResult.name}");
                    };
                    break;
                case Category.Track:
                    if (query.Contains(" - ")) response = await GetTrackInfoAsync(query);
                    else
                    {
                        var searchResult = await SearchLastFm(category, query);
                        response = await GetTrackInfoAsync($"{searchResult.artist} - {searchResult.name}");
                    }
                    break;
                default:
                    return null;
            }

            return response;
        }

        private async Task<dynamic> GetArtistInfoAsync(string query)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=artist.getInfo&artist={query}" +
                $"&api_key={lastFmKey}&autocorrect=1&format=json";
            var response = await GetLastFmData(url);
            
            return response.artist == null ? null : response;
        }

        private async Task<dynamic> GetAlbumInfoAsync(string query)
        {
            var names = await ExtractNames(query);
            var url = 
                $"http://ws.audioscrobbler.com/2.0/?method=album.getInfo&artist={names.Artist}&album={names.Title}" +
                $"&api_key={lastFmKey}&autocorrect=1&format=json";
            var response = await GetLastFmData(url);

            return response.album == null ? null : response;
        }

        private async Task<dynamic> GetTrackInfoAsync(string query)
        {
            var names = await ExtractNames(query);
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=track.getInfo&artist={names.Artist}&track={names.Title}" +
                $"&api_key={lastFmKey}&autocorrect=1&format=json";
            var response = await GetLastFmData(url);

            return response.track == null ? null : response;
        }

        private static Task<(string Artist, string Title)> ExtractNames(string query)
        {
            var dashIndex = query.IndexOf(" - ", StringComparison.Ordinal);
            query = query.Remove(dashIndex, 1);
            query = query.Remove(dashIndex + 1, 1);

            return Task.FromResult((query.Substring(0, dashIndex), query.Substring(dashIndex + 1)));
        }

        private async Task<dynamic> SearchLastFm(Category searchType, string query)
        {
            var search = searchType.ToString().ToLower();
            var url = $"http://ws.audioscrobbler.com/2.0/?method={search}.search&{search}={query}" +
                      $"&api_key={lastFmKey}&format=json";
            var searchResults = await GetLastFmData(url);

            return searchResults.results[$"{search}matches"][search][0];
        }
    }

    public enum Category
    {
        Artist,
        Track,
        Album
    }
}
