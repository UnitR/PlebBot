using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot
{
    public partial class Program
    {
        private readonly Repository<Server> serverRepo = new Repository<Server>();

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            var context = new SocketCommandContext(client, message);

            var condition = $"\"DiscordId\" = {context.Guild.Id}";
            var server = await serverRepo.FindFirst(condition);
            var prefix = server.Prefix;

            if (prefix == null) return;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var result = await commands.ExecuteAsync(context, argPos, provider);

            Error(result, context);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private async void Error(IResult result, SocketCommandContext context)
        {
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task HandleJoinGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            var id = guild.Id;
            await serverRepo.Add("DiscordId", id);
        }

        private async Task HandleLeaveGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            var condition = $"\"DiscordId\" = {guild.Id}";
            await serverRepo.DeleteFirst(condition);
        }
    }
}
