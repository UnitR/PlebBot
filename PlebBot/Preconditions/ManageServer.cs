using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlebBot.Preconditions
{
    internal class ManageServer : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            Debug.Assert(user != null, "user != null");
            if (user.GuildPermissions.Administrator)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(
                    PreconditionResult.FromError(
                        "You need to be a server administrator in order to change the prefix."));
            }
        }
    }
}
