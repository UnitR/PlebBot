using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace PlebBot.Modules
{
    class Admin : ModuleBase<SocketCommandContext>
    {
        private IConfigurationRoot _config;

        public Admin(IConfigurationRoot config)
        {
            this._config = config;
        }

        //TODO: check prefix and embed response
        [Command("prefix")]
        [Summary("Change the command prefix")]
        public async Task ChangePrefix([Summary("The prefix you want to use")] string prefix)
        {
            _config["prefix"] = prefix;
            await ReplyAsync("Successfully changed the prefix.");
        }
    }
}