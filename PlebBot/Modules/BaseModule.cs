using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Caches.CommandCache;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;

namespace PlebBot.Modules
{
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
            var user = await repo.FindByDiscordId((long) Context.User.Id);
            return user;
        }

        protected async Task SaveUserData(string column, object value)
            => await SaveUserData(new[] {column}, new[] {value});

        private async Task SaveUserData(IEnumerable<string> columns, IEnumerable<object> values)
        {
            var userRepo = new Repository<User>();
            var userId = (long) Context.User.Id;

            var user = await userRepo.FindByDiscordId(userId);
            if (user != null)
            {
                await userRepo.UpdateFirst(columns, values, "Id", user.Id);
            }
            else
            {
                var columnsList = columns.ToList();
                columnsList.Add("DiscordId");
                var valuesList = values.ToList();
                valuesList.Add(userId);
                await userRepo.Add(columnsList, valuesList);
            }
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