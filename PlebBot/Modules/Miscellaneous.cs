using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    class Miscellaneous : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Ping!... Pong!")]
        public async Task PingPong()
        {
            await ReplyAsync("...Pong!");
        }

        [Command("bless")]
        [Summary("Blessed be the rains down in Africa")]
        public async Task Bless([Summary("User to bless the rains with")] SocketUser user = null)
        {
            if (user != null)
            {
                await ReplyAsync($"Bless you, {user.Mention} :pray:");
            }
            else
            {
                await ReplyAsync($"Bless :pray:");
            }
        }

        [Command("choose")]
        [Summary("Makes a decision for you")]
        public async Task Choose([Summary("The options you want to choose from")] params string[] choices)
        {
            var options = new List<string>();
            if (choices.Length > 1)
            {
                for(int i = 0; i < choices.Length; i++)
                {
                    if (choices[i].EndsWith(','))
                    {
                        choices[i] = choices[i].Remove(choices[i].Length - 1);
                    }
                    options.Add(choices[i]);
                }
                await PickRandom(options);
            }
            else if (choices.Length == 1 && choices[0].Contains(","))
            {
                options = choices[0].Split(',').ToList();
                if (options.Contains(""))
                {
                    options.Remove("");
                }
                await PickRandom(options);
            }
            else
            {
                await Response.Error(Context, "You must provide a comma-separated list of options.");
            }
        }

        //choose a random element from a list and send the result
        private async Task PickRandom(IReadOnlyList<string> options)
        {
            var select = new Random().Next(0, options.Count);
            var response = new EmbedBuilder()
                .WithTitle("I choose...")
                .WithDescription(options[select])
                .WithColor(Color.DarkGreen);

            await ReplyAsync("", false, response.Build());
        }
    }
}