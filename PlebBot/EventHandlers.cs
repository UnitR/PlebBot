using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PlebBot.Data;
using PlebBot.Data.Models;
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

            //var server = await _context.Servers.SingleOrDefaultAsync(s => s.DiscordId == context.Guild.Id.ToString());

            string prefix;
            using (var conn = BotContext.OpenConnection())
            {
                var id = context.Guild.Id.ToString();
                prefix =
                    await conn.QueryFirstAsync<string>(
                        "select \"Prefix\" from public.\"Servers\" where \"DiscordId\" = @discordId",
                        new {discordId = id});
            }

            if ( prefix == null) return;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot) return;

            var typingState = message.Channel.EnterTypingState();
            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand && result.Error != CommandError.BadArgCount)
            {
                await Response.Error(context, result.ErrorReason);
            }
            typingState.Dispose();
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

            using (var conn = BotContext.OpenConnection())
            {
                var id = guild.Id.ToString();
                await conn.QuerySingleOrDefaultAsync<Server>(
                    "insert into public.\"Servers\" (\"DiscordId\") values (@discordId)",
                    new {discordId = id});
            }
        }

        private async Task HandleLeaveGuildAsync(SocketGuild guild)
        {
            if (guild == null) return;

            using (var conn = BotContext.OpenConnection())
            {
                var id = guild.Id.ToString();
                await conn.QueryAsync("delete from public.\"Servers\" where \"DiscordId\" = @discordId",
                    new {discordId = id});
            }
        }
    }
}
