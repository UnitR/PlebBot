using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace PlebBot.Caches.CommandCache
{
    public static class CommandCacheExtensions
    {
        public static DiscordSocketClient UseCommandCache(this DiscordSocketClient client, IServiceCollection services, int capacity)
        {
            services.AddSingleton(new CommandCacheService(client, capacity));
            return client;
        }
        public static async Task<IUserMessage> SendCachedMessageAsync(this IMessageChannel channel, CommandCacheService cache, ulong commandId, string text, bool prependZWSP = false)
        {
            var message = await channel.SendMessageAsync(prependZWSP ? "\x200b" + text : text);
            cache.Add(commandId, message.Id);

            return message;
        }

        public static ConcurrentBag<T> AddMany<T>(this ConcurrentBag<T> bag, IEnumerable<T> values)
        {
            foreach (var item in values)
            {
                bag.Add(item);
            }
            return bag;
        }
    }
}
