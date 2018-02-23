using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace PlebBot.Helpers
{
    public static class Response
    {
        public static async Task Success(SocketCommandContext context, string successText)
        {
            var response = new EmbedBuilder()
                .AddField("Success", successText)
                .WithColor(Color.Green);

            await context.Channel.SendMessageAsync("", false, response.Build());
        }

        public static async Task Error(SocketCommandContext context, string errorText)
        {
            var response = new EmbedBuilder()
                .AddField("Error", errorText)
                .WithColor(Color.DarkRed);

            await context.Channel.SendMessageAsync("", false, response.Build());
        }
    }
}
