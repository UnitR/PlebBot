using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;
using PlebBot.Helpers;

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
            var context = new SocketCommandContext(_client, message);

            var condition = $"\"DiscordId\" = {context.Guild.Id}";
            var server = await serverRepo.FindFirst(condition);
            var prefix = server.Prefix;

            if (prefix == null) return;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            Error(result, context);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private async void Error(IResult result, SocketCommandContext context)
        {
            if(!result.IsSuccess)
                await Response.Error(context, result.ErrorReason);
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
