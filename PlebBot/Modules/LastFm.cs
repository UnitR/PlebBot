using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.Extensions.Configuration;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    [Name("last.fm")]
    [Alias("fm")]
    class LastFm : ModuleBase<SocketCommandContext>
    {
        private readonly LastfmClient _client;
        private readonly BotContext _dbContext;

        public LastFm(BotContext dbContext)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this._client = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
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
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    var timeSpan = DetermineSpan(span);
                    await GetTopArtistsAsync(username, timeSpan);
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
                    var timeSpan = DetermineSpan(span);
                    await GetTopArtistsAsync(user.LastFm, timeSpan);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("top albums")]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    var timeSpan = DetermineSpan(span);
                    await GetTopAlbumsAsync(username, timeSpan);
                    return;
                }
                await Response.Error(Context, LastFmError.NotFound);
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    var timeSpan = DetermineSpan(span);
                    await GetTopAlbumsAsync(user.LastFm, timeSpan);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        private async Task GetTopAlbumsAsync(string username, LastStatsTimeSpan span)
        {
            var albums = await _client.User.GetTopAlbums(username, span, 1, 10);
            var list = "";
            var i = 1;
            foreach (var album in albums)
            {
                list += $"{i}. {album.ArtistName} - *{album.Name}* [{album.PlayCount} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "albums", span);
        }

        private async Task GetTopArtistsAsync(string username, LastStatsTimeSpan span)
        {
            var artists = await _client.User.GetTopArtists(username, span, 1, 10);
            var list = "";
            var i = 1;
            foreach (var artist in artists)
            {
                list += $"{i}. {artist.Name} [{artist.PlayCount} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "artists", span);
        }

        //determines the time span used for the chart
        private LastStatsTimeSpan DetermineSpan(string span)
        {
            LastStatsTimeSpan timeSpan = LastStatsTimeSpan.Overall;
            switch (span)
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
                case "":
                    break;
                case "overall":
                    break;
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
    }
}