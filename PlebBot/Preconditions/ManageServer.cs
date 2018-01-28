using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Discord;

namespace PlebBot.Preconditions
{
    class ManageServer : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user.GuildPermissions.Administrator)
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                return PreconditionResult.FromError(
                    "You need to be a server administrator in order to change the prefix.");
            }
        }
    }
}
