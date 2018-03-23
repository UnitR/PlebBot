using System.Diagnostics;
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
            var argPos = 0;
            var context = new SocketCommandContext(client, message);

            var condition = $"\"DiscordId\" = {context.Guild.Id}";
            var server = await serverRepo.FindFirst(condition);
            var prefix = server.Prefix;

            if (prefix == null) return;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var result = await commands.ExecuteAsync(context, argPos, ConfigureServices(services));
            Error(result, context);

            commands.Log += msg =>
            {
                if (!(msg.Exception is CommandException ex)) return Task.CompletedTask;

                var channel = client.GetChannel(417956085253668864) as ISocketMessageChannel;
                channel?.SendMessageAsync("Fuck you <@164102776035475458> fix me!!!!1\n" +
                                          $"{ex.Message}\n\n" +
                                          $"{ex.InnerException.ToString()}\n");
                return Task.CompletedTask;
            };
        }

        [Conditional("DEBUG")]
        private static void Error(IResult result, ICommandContext context)
        {
            if (!result.IsSuccess)
                context.Channel.SendMessageAsync(result.ErrorReason);
        }


        private async Task HandleJoinGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            var id = (long) guild.Id;
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
