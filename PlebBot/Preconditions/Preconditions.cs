using System.Threading.Tasks;
using Discord.Commands;

namespace PlebBot.Preconditions
{
    public static class Preconditions
    {
        public static async Task<bool> InCharposting(ICommandContext context)
        {
            var id = context.Channel.Id;
            if (context.Guild.Id != 238003175381139456 || id == 276042631270629376 || id == 314664843892228096 
                || id == 417956085253668864) return true;
            await context.Channel.SendMessageAsync(
                "Let's move to <#276042631270629376> or <#314664843892228096> for this one.");
            return false;
        }
    }
}