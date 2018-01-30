using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.Extensions.Configuration;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    [Group("fm")]
    class LastFm : ModuleBase<SocketCommandContext>
    {
        private readonly LastfmClient _client;
        private readonly BotContext _context;

        public LastFm(BotContext botcontext)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this._client = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
            this._context = botcontext;
        }

        [Command]
        [Summary("Show what you're listening to")]
        public async Task Scrobble([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExists(username))
                {
                    await NowPlaying(username);
                }
                else
                {
                    await Response.Error(Context,"User not found.");
                }
            }
            else
            {
                var user = await _context.Users.SingleOrDefaultAsync(
                    u => u.DiscordId == Context.User.Id.ToString());
                if (user != null)
                {
                    await NowPlaying(user.LastFm);
                }
                else
                {
                    await Response.Error(Context, "You haven't linked your last.fm profile.");
                }
            }
        }

        [Command("set")]
        [Summary("Link your last.fm username to your profile")]
        public async Task SaveUser([Summary("Your last.fm username")] string username = "")
        {
            if (username != string.Empty)
            {
                if (await CheckIfUserExists(username))
                {
                    try
                    {
                        var user = await _context.Users.SingleOrDefaultAsync(
                            u => u.DiscordId == Context.User.Id.ToString());
                        if (user != null)
                        {
                            user.LastFm = username;
                            _context.Update(user);
                            await _context.SaveChangesAsync();

                            await Response.Success(Context, "Succesfully updated your last.fm username.");
                        }
                        else
                        {
                            user = new User()
                            {
                                DiscordId = Context.User.Id.ToString(),
                                LastFm = username
                            };

                            _context.Add(user);
                            await _context.SaveChangesAsync();

                            await Response.Success(
                                Context, "last.fm username saved. You can now freely use the `fm` commands.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Response.Error(
                            Context,"Something has gone terribly wrong. Get on it <@164102776035475458>");
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

        private async Task<bool> CheckIfUserExists(string username)
        {
            var response = await _client.User.GetInfoAsync(username);
            if (response.Success && response.Content.Id != "")
                return true;

            return false;
        }

        //TODO: last.fm user profile picture
        private async Task NowPlaying(string username)
        {
            try
            {
                var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);
                var tracks = response.Content;

                string currAlbum = tracks[0].AlbumName ?? "";
                string prevAlbum = tracks[1].AlbumName ?? "";
                string albumArt = (tracks[0].Images.Large != null) ? tracks[0].Images.Largest.ToString() : "";

                var msg = new EmbedBuilder();
                msg.WithTitle($"Recent tracks for {username}")
                    .WithThumbnailUrl(albumArt)
                    .WithUrl($"https://www.last.fm/user/{username}")
                    .AddField("**Current:**",
                        $"{response.Content[0].ArtistName} - {response.Content[0].Name} " +
                        $"[{currAlbum}]")
                    .AddField("**Previous:**",
                        $"{response.Content[1].ArtistName} - {response.Content[1].Name} " +
                        $"[{prevAlbum}]")
                    .WithColor(Color.DarkBlue);

                await ReplyAsync("", false, msg.Build());
            }
            catch (Exception ex)
            {
                await Response.Error(Context, ex.Message);
            }
        }
    }
}