using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace PlebBot.Modules
{
    class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Ping!... Pong!")]
        public async Task PingPong()
        {
            await ReplyAsync("...Pong!");
        }

        [Command("say")]
        [Summary("Echos a message.")]
        public async Task SayAsync([Remainder] [Summary("The text to echo")] string echo)
        {
            // ReplyAsync is a method on ModuleBase
            await ReplyAsync(echo);
        }

        [Command("name")]
        public async Task SayMyName()
        {
            await ReplyAsync(Context.User.Id.ToString());
        }
    }
}