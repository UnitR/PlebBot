using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;
using PlebBot.Preconditions;

namespace PlebBot.Modules
{
    [Group("Server")]
    [Alias("s")]
    [Summary("Manage server settings")]
    [ManageServer]
    public class Admin : BaseModule
    {
        private readonly Repository<Server> serverRepo;

        public Admin(Repository<Server> repo)
        {
            serverRepo = repo;
        }

        [Command("prefix")]
        [Summary("Change the command prefix")]
        [ManageServer]
        public async Task ChangePrefix([Summary("The prefix you want to use")] string prefix)
        {
            var serverId = Context.Guild.Id;
            var condition = $"\"DiscordId\" = {serverId}";
            var server = await serverRepo.FindFirst(condition);

            if (server != null)
            {
                await serverRepo.UpdateFirst("Prefix", prefix, $"\"Id\" = {server.Id}");
                await Success("Successfully updated the prefix for the server.");
            }
        }
    }
}