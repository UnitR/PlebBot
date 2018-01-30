using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Data.Models;

namespace PlebBot.Modules
{
    class Users : ModuleBase<SocketCommandContext>
    {
        [Command("color")]
        [Alias("colour")]
        [Summary("Change your the colour of your name")]
        public async Task ChangeColour(string colour)
        {
            var roles = Context.Guild.Roles;
            List<SocketRole> rolesList = new List<SocketRole>();
            foreach (var role in roles)
            {
                //if(role.Id.ToString() != )
            }
        }
    }
}
