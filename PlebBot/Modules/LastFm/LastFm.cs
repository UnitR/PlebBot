﻿using System;
using System.Threading.Tasks;
using Dapper;
using Discord.Commands;
using IF.Lastfm.Core.Api;
using Microsoft.Extensions.Configuration;
using PlebBot.Data;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    public partial class LastFm : ModuleBase<SocketCommandContext>
    {
        private readonly LastfmClient _client;
        private readonly string _lastFmKey;

        public LastFm()
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this._client = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
            this._lastFmKey = config["tokens:lastfm_key"];
        }

        [Command("fm")]
        [Summary("Show what you're listening to")]
        public async Task Scrobble([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    await NowPlayingAsync(username);
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

        [Command("fm set")]
        [Summary("Link your last.fm username to your profile")]
        public async Task SaveUser([Summary("Your last.fm username")] string username)
        {
            if (username != null)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    try
                    {
                        using (var conn = BotContext.OpenConnection())
                        {
                            var userId = 
                                await conn.QueryFirstOrDefaultAsync<int>(
                                "select \"Id\" from public.\"Users\" where \"DiscordId\" = @DiscordId",
                                new {DiscordId = Context.User.Id.ToString()});

                            if (userId != 0)
                            {
                                await conn.ExecuteAsync(
                                    "update public.\"Users\" set \"LastFm\" = @lastFm where \"Id\" = @id",
                                    new {lastFm = username, id = userId});

                                await Response.Success(Context, "Succesfully updated your last.fm username.");
                            }
                            else
                            {
                                var discord = Context.User.Id.ToString();
                                await conn.ExecuteAsync(
                                    "insert into public.\"Users\" (\"DiscordId\", \"LastFm\") " +
                                    "values (@discordId, @lastFm)",
                                    new {discordId = discord, lastFm = username});

                                await Response.Success(
                                    Context, "last.fm username saved. You can now freely use the `fm` commands.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await Response.Error(
                            Context,$"Something has gone terribly wrong. Get on it <@164102776035475458>\n\n" +
                                    $"{ex.Message}");
                    }
                }
            }
            else
            {
                await Response.Error(Context, "You must provide a username.");
            }
        }

        [Command("fm top artists")]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of artists to show. Maximum 25. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopArtistsAsync(username, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                }
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopArtistsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("fm top albums")]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of albums to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopAlbumsAsync(username, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                }
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopAlbumsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("fm top tracks")]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of tracks to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await TopTracksAsync(span, username, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                }
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await TopTracksAsync(span, user.LastFm, lim);
                        return;
                    }
                    await Response.Error(Context, LastFmError.Limit);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }

        [Command("fmyt")]
        [Summary("Send a YtService link to your current scrobble")]
        public async Task YtLink([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    await SendYtLinkAsync(username);
                }
            }
            else
            {
                var user = await DbFindUserAsync();
                if (user != null)
                {
                    await SendYtLinkAsync(user.LastFm);
                    return;
                }
                await Response.Error(Context, LastFmError.NotLinked);
            }
        }
    }
}