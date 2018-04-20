using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;

namespace PlebBot.Modules
{
    [Group("Rate Your Music")]
    [Alias("RYM")]
    public class RYM : BaseModule
    {
        private readonly Repository<User> userRepo;

        public RYM(Repository<User> repo)
        {
            userRepo = repo;
        }

        [Command("set")]
        [Name("rym set")]
        [Summary("Link your Rate Your Music account")]
        public async Task SetUsername([Summary("Your RYM username")] string username)
        {
            if (username != null)
            {
                var user = await FindUserAsync();
                if (user != null)
                {
                    await userRepo.UpdateFirst("Rym", username, "Id", user.Id);
                }
                else
                {
                    var discordId = (long) Context.User.Id;
                    string[] columns = {"DiscordId", "Rym"};
                    object[] values = {discordId, username};
                    await userRepo.Add(columns, values);
                }

                await Success("Succesfully set your RYM username.");
                return;
            }

            await Error("You haven't provided a username.");
        }

        [Command]
        [Name("rym")]
        [Summary("Send a link to your Rate Your Music profile")]
        public async Task LinkProfile()
        {
            var user = await userRepo.FindFirst("DiscordId", (long) Context.User.Id);

            if (user?.Rym == null)
            {
                await Error("You haven't linked your RYM account.");
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
