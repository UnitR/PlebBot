using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Caches.CommandCache;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Modules
{
    //TODO: Save data method for handling updating and saving [set] commands
    public class BaseModule : CommandCacheModuleBase<SocketCommandContext>
    {
        private IDisposable typing;

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

        protected async Task<User> FindUserAsync()
        {
            var repo = new Repository<User>();
            var id = Context.User.Id;
            var condition = $"\"DiscordId\" = {id}";
            var user = await repo.FindFirst(condition);

            return user;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            typing = Context.Channel.EnterTypingState();
            base.BeforeExecute(command);
        }

        protected override void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            typing.Dispose();
        }
    }
}