using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlebBot.Services;

namespace PlebBot.Modules
{
    public partial class LastFm
    {
        private async Task HandleCommand(Func<string, Task> func, string username = "")
        {
            if (username != String.Empty)
            {
                if (!await CheckIfUserExistsAsync(username))
                {
                    await Error(NotFound);
                    return;
                }
                await func.Invoke(username);
            }
            else
            {
                var user = await FindUserAsync();
                if (user.LastFm != null)
                {
                    await HandleCommand(func, user.LastFm);
                    return;
                }
                await Error(NotLinked);
            }
        }
        
        private async Task SendChartAsync(ChartType chart, int limit, string span = "", string username = "")
        {
            if (username != String.Empty)
            {
                if (!await CheckIfUserExistsAsync(username))
                {
                    await Error(NotFound);
                    return;
                }
            }
            else
            {
                var user = await FindUserAsync();
                if (user == null)
                {
                    await Error(NotLinked);
                    return;
                }
                username = user.LastFm;
            }

            if (limit < 1 || limit > 25)
            {
                await Error("Check the given limit and try again. Must be between a number between 1 and 25.");
                return;
            }

            var chartSpan = await DetermineSpan(span);
            switch (chart)
            {
                case ChartType.Artists:
                    await GetTopArtistsAsync(username, chartSpan, limit);
                    break;
                case ChartType.Albums:
                    await GetTopAlbumsAsync(username, chartSpan, limit);
                    break;
                case ChartType.Tracks:
                    await TopTracksAsync(span, username, limit);
                    break;
                default:
                    await Error("<@164102776035475458> has fucked up again 🙄");
                    break;
            }
        }

        private async Task SendYtLinkAsync(string username)
        {
            var scrobble = await fmClient.User.GetRecentScrobbles(username, null, 1, 1);
            if (scrobble.TotalItems > 0)
            {
                var track = scrobble.Content[0];
                var ytService = new YtService();
                var response = await ytService.GetVideoLinkAsync(Context, $"{track.ArtistName} {track.Name}");

                if (response != null) await ReplyAsync(response);
                else await Error("No matching videos found.");
                return;
            }
            await Error("You haven't scrobbled any tracks.");
        }

        private async Task GetTopAlbumsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var albums = await fmClient.User.GetTopAlbums(username, span, 1, limit);
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
            await Error($"{username} hasn't scrobbled any albums.");
        }

        private async Task GetTopArtistsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var artists = await fmClient.User.GetTopArtists(username, span, 1, limit);
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
            }
            var list = await GetTopTracksAsync(username, timeSpan, limit);
            var time = await DetermineSpan(span);
            await BuildTopAsync(list, username, "tracks", time);
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
        private async Task BuildTopAsync(string list, string username, string chartType, LastStatsTimeSpan span)
        {
            var totalScrobbles = await TotalScrobblesAsync(span, username);
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span} {totalScrobbles}")
                .WithDescription(list)
                .WithColor(Color.Gold);
            await ReplyAsync("", embed: response.Build());
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

        //show user's scrobbles
        private async Task NowPlayingAsync(string username)
        {
            var response = await fmClient.User.GetRecentScrobbles(username, null, 1, 2);
            if (response.Any())
            {
                var tracks = response.Content;

                var currAlbum = tracks[0].AlbumName ?? "";
                var prevAlbum = tracks[1].AlbumName ?? "";

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

                await ReplyAsync("", embed: msg.Build());
                return;
            }
            await Error("You haven't scrobbled any tracks.");
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
                var url = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" +
                          $"{username}&from={offset}to={now}&api_key={lastFmKey}" +
                          $"&page=1&limit=200&format=json";

                var json = await httpClient.GetStringAsync(url);
                dynamic response = (JObject) JsonConvert.DeserializeObject(json);

                scrobbles = response.recenttracks["@attr"].total;
            }
            return $"[{String.Format("{0:n0}", scrobbles)} scrobbles total]";
        }
    }
}