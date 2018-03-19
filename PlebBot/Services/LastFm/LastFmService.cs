using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Services.LastFm
{
    public class LastFmService
    {
        private readonly string lastFmKey;
        private readonly Repository<User> userRepo;
        private readonly HttpClient httpClient;
        private static string NotFound = "last.fm user not found.";
        private readonly EmbedBuilder errorEmbed = new EmbedBuilder().WithTitle("Error").WithColor(Color.DarkRed);

        public LastFmService(Repository<User> repo, HttpClient client)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            lastFmKey = config["tokens:lastfm_key"];
            userRepo = repo;
            httpClient = client;
        }

        public async Task<EmbedBuilder> GetChartAsync(
            ChartType chart, int limit, string span = "", string username = "", ulong userId = 0)
        {
            if (!await CheckIfUserExistsAsync(username))
                return errorEmbed.WithDescription(NotFound);

            if (limit < 1 || limit > 25)
                return errorEmbed.WithDescription(
                    "Check the given limit and try again. Must be a number between 1 and 25.");

            EmbedBuilder embed;
            span = await DetermineSpan(span);
            switch (chart)
            {
                case ChartType.Artists:
                    embed = await GetTopArtistsAsync(username, span, limit);
                    break;
                case ChartType.Albums:
                    embed = await GetTopAlbumsAsync(username, span, limit);
                    break;
                case ChartType.Tracks:
                    embed = await GetTopTracksAsync(username, span, limit);
                    break;
                default:
                    return errorEmbed.WithDescription("<@164102776035475458> has fucked up again 🙄");
            }
            return embed;
        }

        private async Task<dynamic> GetLastFmData(string call)
        {
            var json = await httpClient.GetStringAsync(call);
            dynamic response = JsonConvert.DeserializeObject(json);
            return response;
        }

        //show user's scrobbles
        public async Task<EmbedBuilder> NowPlayingAsync(string username, ulong userId = 0)
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

            var images = tracks[0].image;
            string albumArt = "";
            for (var i = 3; i >= 0; i++)
            {
                if (tracks[0].image[i] == null) continue;
                albumArt = tracks[0].image[i]["#text"];
                break;
            }

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

        public async Task<EmbedBuilder> SaveUserAsync(string username, ulong userId)
        {
            if (username == null) return errorEmbed.WithDescription("You must provide a username.");
            if (!await CheckIfUserExistsAsync(username)) return errorEmbed.WithTitle(NotFound);

            var findCondition = $"\"DiscordId\" = {(long) userId}";
            var user = await userRepo.FindFirst(findCondition);
            var embed =
                new EmbedBuilder().WithTitle("Success")
                    .WithDescription("Succesfully set your last.fm username.");
            if (user != null)
            {
                var updateCondition = $"\"Id\" = {user.Id}";
                await userRepo.UpdateFirst("LastFm", username, updateCondition);

                return embed;
            }
            string[] columns = {"DiscordId", "LastFm"};
            object[] values = {(long) userId};
            await userRepo.Add(columns, values);

            return embed;
        }

        private async Task<EmbedBuilder> GetTopAlbumsAsync(string username, string span, int limit)
        {
            var call = $"http://ws.audioscrobbler.com/2.0/?method=user.gettopalbums&user={username}" +
                       $"&api_key={lastFmKey}&period={span}&limit={limit}&format=json";
            var response = await GetLastFmData(call);
            if (response.topalbums.album.Count <= 0)
                return errorEmbed.WithDescription($"No scrobbled albums.");

            var list = "";
            var i = 1;
            foreach (var album in response.topalbums.album)
            {
                list += $"{i}. {album.artist.name} - *{album.name}* " +
                        $"[{String.Format("{0:n0}", album.playcount)} scrobbles]\n";
                i++;
            }

            return await BuildTopAsync(list, username, "albums", span);
        }

        private async Task<EmbedBuilder> GetTopArtistsAsync(string username, string span, int limit)
        {
            var call = $"http://ws.audioscrobbler.com/2.0/?method=user.gettopartists&user={username}" +
                       $"&api_key={lastFmKey}&period={span}&limit={limit}&format=json";
            var response = await GetLastFmData(call);
            if (response.topartists.artist.Count <= 0)
                return errorEmbed.WithDescription("No scrobbled artists.");

            var list = "";
            var i = 1;
            foreach (var artist in response.topartists.artist)
            {
                list += $"{i}. {artist.name} [{String.Format("{0:n0}", artist.playcount)} scrobbles]\n";
                i++;
            }
            return await BuildTopAsync(list, username, "artists", span);
        }

        private async Task<EmbedBuilder> GetTopTracksAsync(string username, string span, int limit)
        {
            var call =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={lastFmKey}&format=json";
            dynamic response = await GetLastFmData(call);
            if (response.toptracks.track.Count <= 0)
                return errorEmbed.WithDescription($"No scrobbled tracks.");

            var list = "";
            for (var i = 0; i < limit; i++)
            {
                dynamic track = response.toptracks.track[i];
                list += $"{i + 1}. {track.artist.name} - *{track.name}* " +
                        $"[{String.Format("{0:n0}", track.playcount)} scrobbles]\n";
            }

            return await BuildTopAsync(list, username, "tracks", span);
        }

        //determines the time span used for the chart
        private static Task<string> DetermineSpan(string span)
        {
            switch (span.ToLower())
            {
                case "week":
                case "7days":
                case "7day":
                    span = TimeSpan.Week;
                    break;
                case "month":
                case "30day":
                case "30days":
                    span = TimeSpan.Month;
                    break;
                case "3months":
                case "3month":
                case "90days":
                case "90day":
                    span = TimeSpan.Quarter;
                    break;
                case "6months":
                case "6month":
                    span = TimeSpan.Half;
                    break;
                case "year":
                case "12month":
                case "12months":
                    span = TimeSpan.Year;
                    break;
                default:
                    span = TimeSpan.Overall;
                    break;
            }
            return Task.FromResult(span);
        }

        //builds the embed for the chart
        private async Task<EmbedBuilder> BuildTopAsync(string list, string username, string chartType, string span)
        {
            var totalScrobbles = await TotalScrobblesAsync(span, username);
            span = await FormatSpan(span);
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span} {totalScrobbles}")
                .WithDescription(list)
                .WithColor(Color.Gold);
            return response;
        }

        private Task<string> FormatSpan(string span)
        {
            switch (span)
            {
                case "overall":
                default:
                    break;
                case "1month":
                    span = "month";
                    break;
                case "3month":
                case "6month":
                    span = $"{span.First()} {span.Substring(1)}s";
                    break;
                case "7day":
                    span = "week";
                    break;
                case "12month":
                    span = "year";
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
            if (response.user != null) return true;
            return false;
        }

        private async Task<string> TotalScrobblesAsync(string span, string username)
        {
            var scrobbles = 0;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long offset = 0;
            switch (span)
            {
                case "7day":
                    offset = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
                    break;
                case "1month":
                    offset = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
                    break;
                case "3month":
                    offset = DateTimeOffset.UtcNow.AddDays(-91).ToUnixTimeSeconds();
                    break;
                case "6month":
                    offset = DateTimeOffset.UtcNow.AddDays(-182).ToUnixTimeSeconds();
                    break;
                case "12month":
                    offset = DateTimeOffset.UtcNow.AddDays(-365).ToUnixTimeSeconds();
                    break;
                default:
                    break;
            }

            var call = "http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" +
                        $"{username}&from={offset}&to={now}&api_key={lastFmKey}&format=json";
            var response = await GetLastFmData(call);
            scrobbles = response.recenttracks["@attr"].total;

            return $"[{String.Format("{0:n0}", scrobbles)} scrobbles total]";
        }
    }

    internal static class TimeSpan
    {
        internal static string Overall => "overall";
        internal static string Week => "7day";
        internal static string Month => "1month";
        internal static string Quarter => "3month";
        internal static string Half => "6month";
        internal static string Year => "12month";
    }
}