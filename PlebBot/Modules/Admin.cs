using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Preconditions;

namespace PlebBot.Modules
{
    class Admin : ModuleBase<SocketCommandContext>
    {
        private readonly BotContext _context;

        public Admin(BotContext context)
        {
            this._context = context;
        }

        //TODO: Make sure every server uses its own prefix. But does it even matter if it's only going to be used only in PMCD for a very long time if not forever?
        [Command("prefix")]
        [Summary("Change the command prefix")]
        [ManageServer]
        public async Task ChangePrefix([Summary("The prefix you want to use")] string prefix)
        {
            var serv = await _context.Servers.SingleOrDefaultAsync(
                s => s.DiscordId == Context.Guild.Id.ToString());
            if (serv != null)
            {
                serv.Prefix = prefix;
                _context.Update(serv);
                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.Servers.AddAsync(new Server()
                {
                    DiscordId = Context.Guild.Id.ToString(),
                    Prefix = prefix
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}