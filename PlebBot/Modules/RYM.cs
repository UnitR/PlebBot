using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Modules
{
    [Group("rym")]
    [Alias("RYM", "rateyourmusic")]
    public class RYM : BaseModule
    {
        private readonly Repository<User> userRepo;

        public RYM(Repository<User> repo)
        {
            this.userRepo = repo;
        }

        [Command("set")]
        [Summary("Link your Rate Your Music account")]
        public async Task SetUsername([Summary("Your RYM username")] string username)
        {
            if (username != null)
            {
                var user = await this.FindUserAsync(this.Context);
                if (user != null)
                {
                    await userRepo.UpdateFirst("Rym", username, $"\"Id\" = {user.Id}");
                }
                else
                {
                    var discordId = (long) Context.User.Id;
                    string[] columns = {"DiscordId", "Rym"};
                    object[] values = {discordId, username};
                    await userRepo.Add(columns, values);
                }

                await this.Success("Succesfully set your RYM username.");
                return;
            }

            await this.Error("You haven't provided a username.");
        }

        [Command]
        [Summary("Send a link to your Rate Your Music profile")]
        public async Task LinkProfile()
        {
            var userId = Context.User.Id;
            var condition = $"\"DiscordId\" = {userId}";
            var user = await userRepo.FindFirst(condition);

            if (user?.Rym == null)
            {
                await this.Error("You haven't linked your RYM account.");
                return;
            }

            var response = new EmbedBuilder();
            response
                .WithTitle($"{Context.User.Username}'s RateYourMusic profile:")
                .WithDescription($"https://rateyourmusic.com/~{user.Rym}")
                .WithColor(Color.DarkBlue);

            await ReplyAsync("", embed: response.Build());
        }
    }
}
