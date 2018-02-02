using System;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Helpers;
using Newtonsoft.Json;
using System.Net;

namespace PlebBot.Modules
{
    [Name("last.fm")]
    [Alias("fm")]
    public class LastFm : ModuleBase<SocketCommandContext>
    {
        private readonly LastfmClient _client;
        private readonly BotContext _dbContext;
        private readonly string _lastFmKey;

        public LastFm(BotContext dbContext)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this._client = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
            this._lastFmKey = config["tokens:lastfm_key"];
            this._dbContext = dbContext;
        }

        [Command]
        [Summary("Show what you're listening to")]
        public async Task Scrobble([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    await NowPlayingAsync(username);
                }
                else
                {
                    await Response.Error(Context, LastFmError.NotFound);
                }
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    await NowPlayingAsync(user.LastFm);
                }
                else
                {
                    await Response.Error(Context, LastFmError.NotLinked);
                }
            }
        }

        [Command("set")]
        [Summary("Link your last.fm username to your profile")]
        public async Task SaveUser([Summary("Your last.fm username")] string username)
        {
            if (username != null)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    try
                    {
                        var user = await _dbContext.Users.SingleOrDefaultAsync(
                            u => u.DiscordId == Context.User.Id.ToString());
                        if (user != null)
                        {
                            user.LastFm = username;
                            _dbContext.Update(user);
                            await _dbContext.SaveChangesAsync();

                            await Response.Success(Context, "Succesfully updated your last.fm username.");
                        }
                        else
                        {
                            user = new User()
                            {
                                DiscordId = Context.User.Id.ToString(),
                                LastFm = username
                            };

                            _dbContext.Add(user);
                            await _dbContext.SaveChangesAsync();

                            await Response.Success(
                                Context, "last.fm username saved. You can now freely use the `fm` commands.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Response.Error(
                            Context,$"Something has gone terribly wrong. Get on it <@164102776035475458>\n\n" +
                                    $"{ex.Message}");
                    }
                }
                else
                {
                    await Response.Error(Context, "User not found.");
                }
            }
            else
            {
                await Response.Error(Context, "You must provide a username.");
            }
        }

        [Command("top artists")]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists(
            [Summary("Number of artists to show. Maximum 25. Default is 10.")] string limit = "10",
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = DetermineSpan(span);
                        await GetTopArtistsAsync(username, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotFound);
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = DetermineSpan(span);
                        await GetTopArtistsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("top albums")]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Number of albums to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = DetermineSpan(span);
                        await GetTopAlbumsAsync(username, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotFound);
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = DetermineSpan(span);
                        await GetTopAlbumsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("top tracks")]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks(
            [Summary("Number of tracks to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await SendTopTracks(span, username, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotFound);
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await SendTopTracks(span, user.LastFm, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        private async Task GetTopAlbumsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var albums = await _client.User.GetTopAlbums(username, span, 1, limit);
            var list = "";
            var i = 1;
            foreach (var album in albums)
            {
                list += $"{i}. {album.ArtistName} - *{album.Name}* [{album.PlayCount} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "albums", span);
        }

        private async Task GetTopArtistsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var artists = await _client.User.GetTopArtists(username, span, 1, limit);
            var list = "";
            var i = 1;
            foreach (var artist in artists)
            {
                list += $"{i}. {artist.Name} [{artist.PlayCount} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "artists", span);
        }

        //send the embed with the top tracks
        private async Task SendTopTracks(string span, string username, int limit)
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
                case "year":
                    timeSpan = "12month";
                    break;
            }
            var list = await GetTopTracksAsync(username, timeSpan, limit);
            var time = DetermineSpan(span);
            await BuildTopAsync(list, username, "tracks", time);

        }

        private async Task<string> GetTopTracksAsync(string username, string span, int limit)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={_lastFmKey}&format=json";
            var json = "";
            using (WebClient wc = new WebClient())
            {
                json = await wc.DownloadStringTaskAsync(url);
            }
            dynamic response = JsonConvert.DeserializeObject(json);
            var list = "";
            dynamic track;
            for (int i = 0; i < limit; i++)
            {
                track = response.toptracks.track[i];
                list += $"{i + 1}. {track.artist.name} - *{track.name}* [{track.playcount} scrobbles]\n";
            }

            return list;
        }

        //determines the time span used for the chart
        private LastStatsTimeSpan DetermineSpan(string span)
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
                case "year":
                    timeSpan = LastStatsTimeSpan.Year;
                    break;
                //case "":
                //    break;
                //case "overall":
                //    break;
            }

            return timeSpan;
        }

        //builds the embed for the chart
        private async Task BuildTopAsync(string list, string username, string chartType, LastStatsTimeSpan span)
        {
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span}")
                .WithDescription(list)
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync("", false, response);
        }

        //Checks if the last.fm user exists
        private async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var response = await _client.User.GetInfoAsync(username);
            if (response.Success && response.Content.Id != "")
                return true;

            return false;
        }

        //TODO: last.fm user profile picture
        //show user's scrobbles
        private async Task NowPlayingAsync(string username)
        {
            try
            {
                var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);
                var tracks = response.Content;

                string currAlbum = tracks[0].AlbumName ?? "";
                string prevAlbum = tracks[1].AlbumName ?? "";
                string albumArt = (tracks[0].Images.Large != null) ? tracks[0].Images.Largest.ToString() : "";

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
            }
            catch (Exception ex)
            {
                await Response.Error(Context, ex.Message);
            }
        }

        //find a user in the database
        private async Task<User> DbFindUserAsync()
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(
                u => u.DiscordId == Context.User.Id.ToString());

            return user;
        }
    }

    static class LastFmError
    {
        public static string NotFound => "User not found.";
        public static string NotLinked => "You haven't linked your last.fm profile.";
        public static string InvalidSpan => "Invalid time span provided.";
        public static string Limit => "Check the given limit and try again.";
    }
}