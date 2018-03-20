using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace PlebBot.Preconditions
{
    public class Chartposting : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {

            if (context.Guild.Id != 238003175381139456)
                return Task.FromResult(PreconditionResult.FromSuccess());
            if (context.Channel.Id == 276042631270629376 || context.Channel.Id == 314664843892228096)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(
                    $"Head over to <#276042631270629376> or <#314664843892228096> to use that command."));
        }
    }
}