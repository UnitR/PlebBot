using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.CommandCache;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot
{
    public class BaseModule : CommandCacheModuleBase<SocketCommandContext>
    {
        protected async Task Success(string successText)
        {
            var response = new EmbedBuilder()
                .AddField("Success", successText)
                .WithColor(Color.Green);

            await ReplyAsync("", embed: response.Build());
        }

        protected async Task Error(string errorText)
        {
            var response = new EmbedBuilder()
                .AddField("Error", errorText)
                .WithColor(Color.DarkRed);

            await ReplyAsync("", embed: response.Build());
        }

        protected async Task<User> FindUserAsync(ICommandContext context)
        {
            var repo = new Repository<User>();
            var id = context.User.Id;
            var condition = $"\"DiscordId\" = {id}";
            var user = await repo.FindFirst(condition);

            return user;
        }
    }
}