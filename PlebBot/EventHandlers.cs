using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;
using System.Threading;

namespace PlebBot
{
    public partial class Program
    {
        private readonly Repository<Server> serverRepo = new Repository<Server>();

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(client, message);

            var server = await serverRepo.FindByDiscordId((long) context.Guild.Id);
            var prefix = server.Prefix;

            if (prefix == null) return;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var result = await commands.ExecuteAsync(context, argPos, ConfigureServices(services));
            
            if(!result.IsSuccess && result.ErrorReason.Contains("Timeout"))
            {
                var errorMessage = await context.Channel.SendMessageAsync("Slow down a little.");
                Thread.Sleep(5000);
                await errorMessage.DeleteAsync();
                await message.DeleteAsync();
            }

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

        //[Conditional("DEBUG")]
        //private static void Error(IResult result, ICommandContext context)
        //{
        //    if (!result.IsSuccess)
        //        context.Channel.SendMessageAsync(result.ErrorReason);
        //}


        private async Task HandleJoinGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            var id = (long) guild.Id;
            await serverRepo.Add("DiscordId", id);
        }

        private async Task HandleLeaveGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;
            await serverRepo.DeleteFirst("DiscordId", guild.Id);
        }
    }
}
