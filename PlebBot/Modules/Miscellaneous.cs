using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

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
        public async Task Bless(SocketUser user = null)
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
    }
}