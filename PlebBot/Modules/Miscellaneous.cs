﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Services;

namespace PlebBot.Modules
{
    public class Miscellaneous : BaseModule
    {
        [Command("ping")]
        [Summary("Used for testing connection")]
        public async Task PingPong()
        {
            await ReplyAsync("...Pong!");
        }

        [Command("bless", RunMode = RunMode.Async)]
        [Summary("Blessed be the rains down in Africa")]
        public async Task Bless([Summary("User to bless the rains with")] [Remainder] string username = "")
        {
            if (username == String.Empty)
            {
                await ReplyAsync("Bless :pray:");
                return;
            }

            var mention = "";
            username = username.ToLowerInvariant();

            if (username == "pleb") mention = @"<@287097793514831873>";
            else
            {
                foreach (var user in Context.Guild.Users)
                {
                    if (user.Username.ToLowerInvariant().Contains(username))
                    {
                        mention = user.Mention;
                        break;
                    }

                    if (user.Nickname == null || !user.Nickname.ToLowerInvariant().Contains(username)) continue;

                    mention = user.Mention;
                    break;
                }
            }
            await ReplyAsync($"Bless you, {mention} :pray:");
        }

        [Priority(1)]
        [Command("bless")]
        [Summary("Blessed be the rains down in Africa")]
        public async Task BlessUser([Summary("User to bless the rains with")] SocketUser user)
        {
            await ReplyAsync($"Bless you, {user.Mention} :pray:");
        }

        [Command("choose")]
        [Summary("Makes a decision for you")]
        public async Task Choose([Remainder] [Summary("The options you want to choose from")] string choice_list)
        {
            var options = choice_list.Split(',');
            options = options.Where((val, idx) => val.Trim() != "").ToArray();

            if (options.Length > 1) await PickRandom(options);
            else await this.Error("You must provide a comma-separated list of options.");
        }

        [Command("yt", RunMode = RunMode.Async)]
        [Summary("Link a YtService video")]
        public async Task LinkVideo([Remainder] [Summary("The search query")] string query)
        {
            var yt = new YtService();
            var response = await yt.GetVideoLinkAsync(Context, query);
            if (response != null)
            {
                await ReplyAsync(response);
                return;
            }

            await this.Error("No videos found.");
        }

        //choose a random element from a list and send the result
        private async Task PickRandom(IReadOnlyList<string> options)
        {
            var select = new Random().Next(0, options.Count);
            var response = new EmbedBuilder()
                .WithTitle("I choose...")
                .WithDescription(options[select])
                .WithColor(Color.DarkGreen);

            await ReplyAsync("", embed: response.Build());
        }
    }
}
