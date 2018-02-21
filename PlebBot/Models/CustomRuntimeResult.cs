using Discord.Commands;

namespace PlebBot.Models
{
    public class CustomRuntimeResult : RuntimeResult
    {
        public CustomRuntimeResult(CommandError? error, string reason) : base(error, reason)
        {
        }
    }
}