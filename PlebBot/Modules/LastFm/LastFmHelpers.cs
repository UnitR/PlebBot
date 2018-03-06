using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlebBot.Data.Models;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    public partial class LastFm
    {
        private async Task SendYtLinkAsync(string username)
        {
            var scrobble = await _client.User.GetRecentScrobbles(username, null, 1, 1);
            if (scrobble.TotalItems > 0)
            {
                var track = scrobble.Content[0];
                var ytService = new YtService();
                var response = await ytService.LinkVideoAsync(Context, $"{track.ArtistName} {track.Name}");

                if (response != null) Cache.Add(Context.Message.Id, response.Id);
                return;
            }
            await Response.Error(Context, "You haven't scrobbled any tracks.");
        }

        private async Task GetTopAlbumsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var albums = await _client.User.GetTopAlbums(username, span, 1, limit);
            if (albums.TotalItems > 0)
            {
                var list = "";
                var i = 1;

                foreach (var album in albums)
                {
                    list += $"{i}. {album.ArtistName} - *{album.Name}* " +
                            $"[{String.Format("{0:n0}", album.PlayCount)} scrobbles]\n";
                    i++;
                }

                await BuildTopAsync(list, username, "albums", span);
                return;
            }
            await Response.Error(Context, $"{username} hasn't scrobbled any albums.");
        }

        private async Task GetTopArtistsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var artists = await _client.User.GetTopArtists(username, span, 1, limit);
            var list = "";
            var i = 1;
            foreach (var artist in artists)
            {
                list += $"{i}. {artist.Name} [{String.Format("{0:n0}", artist.PlayCount)} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "artists", span);
        }

        private async Task TopTracksAsync(string span, string username, int limit)
        {
            var timeSpan = "overall";
            switch (span.ToLower())
            {
                case "week":
                    timeSpan = "7day";
                    break;
                case "month":
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
            }
            var list = await GetTopTracksAsync(username, timeSpan, limit);
            var time = await DetermineSpan(span);
            await BuildTopAsync(list, username, "tracks", time);
        }

        private async Task<string> GetTopTracksAsync(string username, string span, int limit)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={_lastFmKey}&format=json";
            string json;
            using (WebClient wc = new WebClient())
            {
                json = await wc.DownloadStringTaskAsync(url);
            }
            dynamic response = JsonConvert.DeserializeObject(json);
            var list = "";
            for (int i = 0; i < limit; i++)
            {
                dynamic track = response.toptracks.track[i];
                list += $"{i + 1}. {track.artist.name} - *{track.name}* " +
                        $"[{String.Format("{0:n0}", track.playcount)} scrobbles]\n";
            }
            return list;
        }

        //determines the time span used for the chart
        private Task<LastStatsTimeSpan> DetermineSpan(string span)
        {
            LastStatsTimeSpan timeSpan = LastStatsTimeSpan.Overall;
            switch (span.ToLower())
            {
                case "week":
                    timeSpan = LastStatsTimeSpan.Week;
                    break;
                case "month":
                    timeSpan = LastStatsTimeSpan.Month;
                    break;
                case "3months":
                case "3month":
                    timeSpan = LastStatsTimeSpan.Quarter;
                    break;
                case "6months":
                case "6month":
                    timeSpan = LastStatsTimeSpan.Half;
                    break;
                case "year":
                    timeSpan = LastStatsTimeSpan.Year;
                    break;
            }

            return Task.FromResult(timeSpan);
        }

        //builds the embed for the chart
        private async Task BuildTopAsync(string list, string username, string chartType, LastStatsTimeSpan span)
        {
            var totalScrobbles = await TotalScrobblesAsync(span, username);
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span} {totalScrobbles}")
                .WithDescription(list)
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync("", false, response);
        }

        //Checks if the last.fm user exists
        private async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var response = await _client.User.GetInfoAsync(username);
            if (response.Success)
            {
                if (response.Content.Id != "")
                {
                    if (response.Content.Playcount >= 0)
                        return true;
                }
            }
            await Response.Error(Context, LastFmError.NotFound);
            return false;
        }

        //show user's scrobbles
        private async Task NowPlayingAsync(string username)
        {
            try
            {
                var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);
                if (response.Any())
                {
                    var tracks = response.Content;

                    string currAlbum = tracks[0].AlbumName ?? "";
                    string prevAlbum = tracks[1].AlbumName ?? "";

                    var images = tracks[0].Images;
                    var albumArt = "";
                    if (images != null) albumArt = images.LastOrDefault(img => img != null)?.ToString();

                    var msg = new EmbedBuilder();
                    var currField = $"{response.Content[0].ArtistName} - {response.Content[0].Name}";
                    var prevField = $"{response.Content[1].ArtistName} - {response.Content[1].Name}";
                    if (currAlbum.Length > 0) currField += $" [{currAlbum}]";
                    if (prevAlbum.Length > 0) prevField += $" [{prevAlbum}]";
                    msg.WithTitle($"Recent tracks for {username}")
                        .WithThumbnailUrl(albumArt)
                        .WithUrl($"https://www.last.fm/user/{username}")
                        .AddField("**Current:**", currField)
                        .AddField("**Previous:**", prevField)
                        .WithColor(Color.DarkBlue);

                    await ReplyAsync("", false, msg.Build());
                    return;
                }
                await Response.Error(Context, "You haven't scrobbled any tracks.");
            }
            catch (Exception ex)
            {
                await Response.Error(Context, ex.Message);
            }
        }

        //find a user in the database
        private async Task<User> DbFindUserAsync()
        {
            var id = Context.User.Id.ToString();
            var condition = $"\"DiscordId\" = \'{id}\'";
            var user = await userRepo.FindFirst(condition);

            return user;
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
                    var user = await _client.User.GetInfoAsync(username);
                    scrobbles = user.Content.Playcount;
                    break;
            }

            if (offset != 0)
            {
                var url = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" +
                          $"{username}&from={offset}to={now}&api_key={_lastFmKey}" +
                          $"&page=1&limit=200&format=json";

                string json;
                using (WebClient wc = new WebClient())
                {
                    json = await wc.DownloadStringTaskAsync(url);
                }
                dynamic response = (JObject) JsonConvert.DeserializeObject(json);

                scrobbles = response.recenttracks["@attr"].total;
            }
            return $"[{String.Format("{0:n0}", scrobbles)} scrobbles total]";
        }
    }
}