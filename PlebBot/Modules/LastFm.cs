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
using Microsoft.Extensions.Configuration;
using PlebBot.Data;

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

        //TODO: error thrown for some users (check with unitr)
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
                    await Error("User not found.");
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
                    await Error("You haven't linked your last.fm profile.");
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

                            await Success("Succesfully updated your last.fm username.");
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

                            await Success("last.fm username saved. You can now freely use the !fm commands.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Error("Something has gone terribly wrong. Get on it <@164102776035475458>");
                    }
                }
                else
                {
                    await Error("User not found.");
                }
            }
            else
            {
                await Error("You must provide a username.");
            }
        }

        private async Task<bool> CheckIfUserExists(string username)
        {
            var response = await _client.User.GetInfoAsync(username);
            if (response.Success && response.Content.Id != "")
                return true;

            return false;
        }

        //TODO: Create an enum for the typical last.fm errors (maybe?)
        private async Task Error(string message)
        {
            var msg = new EmbedBuilder()
                .AddField("Error", message)
                .WithColor(Color.DarkRed);

            await ReplyAsync("", false, msg.Build());
        }

        private async Task Success(string message)
        {
            var msg = new EmbedBuilder()
                .AddField("Success", message)
                .WithColor(Color.Green);

            await ReplyAsync("", false, msg.Build());
        }

        //TODO: last.fm user profile picture
        private async Task NowPlaying(string username)
        {
            var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);

            var msg = new EmbedBuilder()
                .WithTitle($"Recent tracks for {username}")
                .WithThumbnailUrl(response.Content[0].Images.Medium.ToString())
                .WithUrl($"https://www.last.fm/user/{username}")
                .AddField("**Current:**",
                    $"{response.Content[0].ArtistName} - {response.Content[0].Name} " +
                    $"[{response.Content[0].AlbumName}]")
                .AddField("**Previous:**",
                    $"{response.Content[1].ArtistName} - {response.Content[1].Name} " +
                    $"[{response.Content[1].AlbumName}]")
                .WithColor(Color.DarkBlue);

            await ReplyAsync("", false, msg);
        }
    }
}