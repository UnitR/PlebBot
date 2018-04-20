using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;

namespace PlebBot.Modules
{
    [Group("Roles")]
    [Alias("Role")]
    [Summary("Manage server roles")]
    public class Roles : BaseModule
    {
        private readonly Repository<Role> roleRepo;
        private readonly Repository<Server> serverRepo;

        public Roles(Repository<Role> roleRepo, Repository<Server> serverRepo)
        {
            this.roleRepo = roleRepo;
            this.serverRepo = serverRepo;
        }

        [Command]
        [Name("roles")]
        [Summary("Get a list of the self-assignable roles")]
        public async Task GetAssignable()
        {
            var roles = await GetServerRolesAsync();
            if (roles.Any())
            {
                var response = new EmbedBuilder()
                    .WithTitle("List of self-assignable roles:")
                    .WithColor(Color.Green);
                var description = "";
                var i = 1;
                foreach (var role in roles)
                {
                    description += $"{i}. {role.Name}\n";
                    i++;
                }
                response.WithDescription(description);

                await ReplyAsync("", embed: response.Build());
            }
            else
            {
                await Error("This server doesn't have any self-assignable roles.");
            }
        }

        [Command]
        [Alias("get")]
        [Name("roles get")]
        [Summary("Get a self-assignable role")]
        public async Task GetRole([Summary("The name of the role you wish to obtain")] string role)
        {
            var roles = await GetServerRolesAsync();
            if (roles.Any())
            {
                var roleResult =
                    roles.Find(r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                if (roleResult != null)
                {
                    var userRoles = (Context.User as IGuildUser)?.RoleIds;
                    var contains = userRoles?.Contains((ulong) roleResult.DiscordId);
                    if (!contains.GetValueOrDefault())
                    {
                        var assign =
                            Context.Guild.Roles.SingleOrDefault(
                                r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                        if (roleResult.IsColour)
                        {
                            var colours = roles.Where(r => r.IsColour).ToList();
                            foreach (var colour in colours)
                            {
                                contains = userRoles?.Contains((ulong) colour.DiscordId);
                                if (!contains.GetValueOrDefault()) continue;
                                var unassign =
                                    Context.Guild.Roles.SingleOrDefault(
                                        r => r.Id == (ulong) colour.DiscordId);

                                await ((IGuildUser)Context.User).RemoveRoleAsync(unassign);
                            }
                        }

                        await ((IGuildUser)Context.User).AddRoleAsync(assign);
                        await Success($"Good job! You managed to get the '{assign?.Name}' role!");
                    }
                    else await Error("You already have this role assigned to you.");
                }
                else await Error($"There isn't a self-assignable role called '{role}'.");
            }
            else await Error("There are no self-assignable roles for the server.");
        }

        [Command("remove")]
        [Name("roles remove")]
        [Summary("Removes a role from you")]
        public async Task RemoveRole([Summary("The name of the role you want to remove")] string role)
        {
            var roleResult = await roleRepo.FindFirst("Name", role.ToLowerInvariant());

            if (roleResult == null)
            {
                await Error($"No role with the name {role} was found.");
                return;
            }

            var user = Context.User as IGuildUser;

            Debug.Assert(user != null, "user != null");
            var userRole = user.RoleIds.FirstOrDefault(r => r == (ulong) roleResult.DiscordId);

            if (userRole == 0)
            {
                await Error($"You don't have '{roleResult.Name}' assigned to you.");
                return;
            }
            var guildRole = Context.Guild.Roles.FirstOrDefault(x => x.Id == userRole);
            await user.RemoveRoleAsync(guildRole);
            await Success($"Removed '{roleResult.Name}' from your roles.");
        }


        [Command("self")]
        [Name("roles self")]
        [Summary("Make a role self-assignable")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetAssignable(
            [Summary("The role you want to make self-assignable")] string role,
            [Summary(
                "Marks the role as a colour role. Enables automatic colour removal and assigning of colours for users. Example: roles self Blue -c")] string colour = "")
        {
            var serverRoles = Context.Guild.Roles.ToList();
            var roleFind =
                serverRoles.Find(r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));

            if (roleFind != null)
            {
                var roleDb = await roleRepo.FindFirst("Name", role.ToLower());
                if (roleDb == null)
                {
                    var server = await serverRepo.FindByDiscordId((long) Context.Guild.Id);
                    var serverId = server.Id;
                    var isColour = colour == "-c";

                    string[] columns = { "ServerId", "DiscordId", "Name", "IsColour" };
                    object[] values = { serverId, (long) roleFind.Id, roleFind.Name, isColour };
                    await roleRepo.Add(columns, values);

                    await Success($"Added '{roleFind.Name}' to the list of self-assignable roles.");
                }
                else
                {
                    await Error($"The '{roleFind.Name}' role is already set as self-assignable.");
                }
            }
            else
            {
                await Error($"No role with the name '{role}' was found in the server.");
            }

        }

        [Command("remove")]
        [Name("roles remove")]
        [Summary("Remove a role from the self-assignable list")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveAssignable([Summary("The role whose name you wish to remove")] string role)
        {
            var roleToRemove = await roleRepo.FindFirst("Name", role.ToLower());
            if (roleToRemove != null)
            {
                await roleRepo.DeleteFirst("Id", roleToRemove.Id);
                await Success(
                    $"The '{roleToRemove.Name}' role has been successfully removed from the self-assignable list");
                return;
            }
            await Error($"No role with the name '{role}' has been found in the self-assignable list");
        }

        private async Task<List<Role>> GetServerRolesAsync()
        {
            var server = await serverRepo.FindByDiscordId((long) Context.Guild.Id);
            var roles = await roleRepo.FindAll("ServerId", server.Id) as List<Role>;

            return roles;
        }
    }
}
