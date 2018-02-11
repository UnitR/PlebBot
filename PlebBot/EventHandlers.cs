using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PlebBot.Helpers;

namespace PlebBot
{
    public partial class Program
    {
        //private readonly MessageCache _botResponses = new MessageCache();

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            var context = new SocketCommandContext(_client, message);

            var server = await _context.Servers.SingleOrDefaultAsync(s => s.DiscordId == context.Guild.Id.ToString());
            if (server == null) return;
            var prefix = server.Prefix;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand && result.Error != CommandError.BadArgCount)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        //TODO: delete bot response if command message is deleted
        //private async Task HandleMessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        //{
        //    var msg = _botResponses.Remove(message.Id);
        //    try
        //    {
        //        await msg.DeleteAsync();
        //    }
        //    catch (Exception) { }
        //}

        private async Task HandleJoinGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            _context.Servers.Add(new Data.Models.Server()
            {
                DiscordId = guild.Id.ToString()
            });
            await _context.SaveChangesAsync();
        }

        private async Task HandleLeaveGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            var server = await _context.Servers.SingleOrDefaultAsync(s => s.DiscordId == guild.Id.ToString());
            if (server != null)
            {
                _context.Servers.Remove(server);
                await _context.SaveChangesAsync();
            }
        }
    }
}
