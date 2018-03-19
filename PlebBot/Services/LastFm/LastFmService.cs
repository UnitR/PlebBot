using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Services.LastFm
{
    public class LastFmService
    {
        private readonly LastfmClient fmClient;
        private readonly string lastFmKey;
        private readonly Repository<User> userRepo;
        private readonly HttpClient httpClient;
        private static string NotLinked => "You haven't linked your last.fm profile.";
        private static string NotFound => "last.fm user not found.";
        private readonly EmbedBuilder errorEmbed = new EmbedBuilder().WithTitle("Error").WithColor(Color.DarkRed);

        public LastFmService(Repository<User> repo, HttpClient client)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            fmClient = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
            lastFmKey = config["tokens:lastfm_key"];
            userRepo = repo;
            httpClient = client;
        }

        public async Task<EmbedBuilder> GetChartAsync(
            ChartType chart, int limit, string span = "", string username = "", ulong userId = 0)
        {
            if (username != String.Empty)
            {
                if (!await CheckIfUserExistsAsync(username)) return errorEmbed.WithDescription(NotFound);
            }
            else
            {
                var condition = $"\"DiscordId\" = {(long) userId}";
                var user = await userRepo.FindFirst(condition);
                if (user.LastFm == null) return errorEmbed.WithDescription(NotLinked);
                username = user.LastFm;
            }

            if (limit < 1 || limit > 25)
                return errorEmbed.WithDescription(
                    "Check the given limit and try again. Must be between a number between 1 and 25.");

            EmbedBuilder embed;
            var chartSpan = await DetermineSpan(span);
            switch (chart)
            {
                case ChartType.Artists:
                    embed = await GetTopArtistsAsync(username, chartSpan, limit);
                    break;
                case ChartType.Albums:
                    embed = await GetTopAlbumsAsync(username, chartSpan, limit);
                    break;
                case ChartType.Tracks:
                    embed = await TopTracksAsync(span, username, limit);
                    break;
                default:
                    return errorEmbed.WithDescription("<@164102776035475458> has fucked up again 🙄");
            }
            return embed;
        }

        //show user's scrobbles
        public async Task<EmbedBuilder> NowPlayingAsync(string username, ulong userId = 0)
        {
            if (username != String.Empty)
            {
                if (!await CheckIfUserExistsAsync(username))
                    return errorEmbed.WithDescription(NotFound);
            }
            else
            {
                var condition = $"\"DiscordId\" = {(long) userId}";
                var user = await userRepo.FindFirst(condition);
                if (user.LastFm == null) return errorEmbed.WithDescription(NotLinked);
                username = user.LastFm;
            }

            var response = await fmClient.User.GetRecentScrobbles(username, null, 1, 2);
            if (!response.Any()) return errorEmbed.WithDescription("You haven't scrobbled any tracks.");

            var tracks = response.Content;
            var currAlbum = tracks[0].AlbumName ?? "";
            var prevAlbum = tracks[1].AlbumName ?? "";
            var images = tracks[0].Images;
            var albumArt = "";
            if (images != null) albumArt = images.LastOrDefault(img => img != null)?.ToString();

            var embed = new EmbedBuilder();
            var currField = $"{response.Content[0].ArtistName} - {response.Content[0].Name}";
            var prevField = $"{response.Content[1].ArtistName} - {response.Content[1].Name}";
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
            var scrobble = await fmClient.User.GetRecentScrobbles(username, null, 1, 1);
            if (scrobble.TotalItems <= 0) return "You haven't scrobbled any tracks.";

            var track = scrobble.Content[0];
            var ytService = new YtService();
            var response = await ytService.GetVideoLinkAsync($"{track.ArtistName} {track.Name}");
            return response;
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

        private async Task<EmbedBuilder> GetTopAlbumsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var albums = await fmClient.User.GetTopAlbums(username, span, 1, limit);

            if (albums.TotalItems <= 0) return errorEmbed.WithDescription($"{username} hasn't scrobbled any albums.");

            var list = "";
            var i = 1;
            foreach (var album in albums)
            {
                list += $"{i}. {album.ArtistName} - *{album.Name}* " +
                        $"[{String.Format("{0:n0}", album.PlayCount)} scrobbles]\n";
                i++;
            }

            return await BuildTopAsync(list, username, "albums", span);
        }

        private async Task<EmbedBuilder> GetTopArtistsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var artists = await fmClient.User.GetTopArtists(username, span, 1, limit);
            var list = "";
            var i = 1;
            foreach (var artist in artists)
            {
                list += $"{i}. {artist.Name} [{String.Format("{0:n0}", artist.PlayCount)} scrobbles]\n";
                i++;
            }

            return await BuildTopAsync(list, username, "artists", span);
        }

        private async Task<EmbedBuilder> TopTracksAsync(string span, string username, int limit)
        {
            string timeSpan;
            switch (span.ToLower())
            {
                case "week":
                    timeSpan = "7day";
                    break;
                case "month":
                case "1month":
                    timeSpan = "1month";
                    break;
                case "3months":
                case "3month":
                    timeSpan = "3month";
                    break;
                case "6months":
                case "6month":
                    timeSpan = "6month";
                    break;
                case "year":
                    timeSpan = "12month";
                    break;
                default:
                    timeSpan = "overall";
                    break;
            }
            var list = await GetTopTracksAsync(username, timeSpan, limit);
            var time = await DetermineSpan(span);
            return await BuildTopAsync(list, username, "tracks", time);
        }

        private async Task<string> GetTopTracksAsync(string username, string span, int limit)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={lastFmKey}&format=json";
            var json = await httpClient.GetStringAsync(url);
            dynamic response = JsonConvert.DeserializeObject(json);

            var list = "";
            for (var i = 0; i < limit; i++)
            {
                dynamic track = response.toptracks.track[i];
                list += $"{i + 1}. {track.artist.name} - *{track.name}* " +
                        $"[{String.Format("{0:n0}", track.playcount)} scrobbles]\n";
            }
            return list;
        }

        //determines the time span used for the chart
        private static Task<LastStatsTimeSpan> DetermineSpan(string span)
        {
            LastStatsTimeSpan timeSpan;
            switch (span.ToLower())
            {
                case "week":
                case "7days":
                case "7day":
                    timeSpan = LastStatsTimeSpan.Week;
                    break;
                case "month":
                case "30day":
                case "30days":
                    timeSpan = LastStatsTimeSpan.Month;
                    break;
                case "3months":
                case "3month":
                case "90days":
                case "90day":
                    timeSpan = LastStatsTimeSpan.Quarter;
                    break;
                case "6months":
                case "6month":
                    timeSpan = LastStatsTimeSpan.Half;
                    break;
                case "year":
                    timeSpan = LastStatsTimeSpan.Year;
                    break;
                default:
                    timeSpan = LastStatsTimeSpan.Overall;
                    break;
            }

            return Task.FromResult(timeSpan);
        }

        //builds the embed for the chart
        private async Task<EmbedBuilder> BuildTopAsync(
            string list, string username, string chartType, LastStatsTimeSpan span)
        {
            var totalScrobbles = await TotalScrobblesAsync(span, username);
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span} {totalScrobbles}")
                .WithDescription(list)
                .WithColor(Color.Gold);
            return response;
        }

        //Checks if the last.fm user exists
        private async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var response = await fmClient.User.GetInfoAsync(username);
            if (!response.Success) return false;
            if (response.Content.Id == "") return false;
            if (response.Content.Playcount >= 0) return true;
            return false;
        }

        private async Task<string> TotalScrobblesAsync(LastStatsTimeSpan span, string username)
        {
            var scrobbles = 0;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long offset = 0;
            switch (span.ToString().ToLower())
            {
                case "week":
                    offset = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
                    break;
                case "month":
                    offset = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
                    break;
                case "quarter":
                    offset = DateTimeOffset.UtcNow.AddDays(-91).ToUnixTimeSeconds();
                    break;
                case "half":
                    offset = DateTimeOffset.UtcNow.AddDays(-182).ToUnixTimeSeconds();
                    break;
                case "year":
                    offset = DateTimeOffset.UtcNow.AddDays(-365).ToUnixTimeSeconds();
                    break;
                default:
                    var user = await fmClient.User.GetInfoAsync(username);
                    scrobbles = user.Content.Playcount;
                    break;
            }

            if (offset != 0)
            {
                var url = "http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" +
                          $"{username}&from={offset}to={now}&api_key={lastFmKey}" +
                          "&page=1&limit=200&format=json";

                var json = await httpClient.GetStringAsync(url);
                dynamic response = (JObject)JsonConvert.DeserializeObject(json);

                scrobbles = response.recenttracks["@attr"].total;
            }
            return $"[{String.Format("{0:n0}", scrobbles)} scrobbles total]";
        }
    }
}