using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PlebBot.Caches;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;

namespace PlebBot
{
    public partial class Program
    {
        private readonly Repository<Server> serverRepo = new Repository<Server>();

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message)) return;
            
            var context = new SocketCommandContext(client, message);
            var server = await serverRepo.FindByDiscordId((long) context.Guild.Id);
            var argPos = 0;
            var prefix = server.Prefix;

            if (prefix == null) return;

            var hasStringPrefix = message.HasStringPrefix(prefix, ref argPos);
            
            if(server.LogEnabled && !hasStringPrefix && !message.Author.IsBot) cache.Add(message.Id, message);
            
            if (!(hasStringPrefix ||
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
            await serverRepo.DeleteFirst("DiscordId", guild.Id);
        }
        
        private readonly MessageCache cache = new MessageCache();
        
        private async Task HandleMessageDeleted(Cacheable<IMessage, ulong> cacheable, ISocketMessageChannel socketMessageChannel)
        {
            var messageChannel = socketMessageChannel as SocketTextChannel;
            var server = await serverRepo.FindByDiscordId((long) messageChannel.Guild.Id);
            
            if (!server.LogEnabled) return;

            var msg = cache.Remove(cacheable.Id);
            await messageChannel.SendMessageAsync(msg.Content);
        }
    }
}
