using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Helpers.CommandCache;

namespace PlebBot.CommandCache
{
    public abstract class CommandCacheModuleBase<TCommandCache, TCacheKey, TCacheValue, TCommandContext> : ModuleBase<TCommandContext>
        where TCommandCache : ICommandCache<TCacheKey, TCacheValue>
        where TCommandContext : class, ICommandContext
    {
        public TCommandCache Cache { get; set; }

        protected override async Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            var response = await Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            Cache.Add(Context.Message, response);
            return response;
        }
    }

    public abstract class CommandCacheModuleBase<TCommandContext> : CommandCacheModuleBase<CommandCacheService, ulong, ConcurrentBag<ulong>, TCommandContext>
        where TCommandContext : class, ICommandContext
    { }
}
