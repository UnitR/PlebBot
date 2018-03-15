using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.Commands;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Helpers;
using PlebBot.Helpers.CommandCache;
using PlebBot.Preconditions;

namespace PlebBot.Modules
{
    public class Admin : CommandCacheModuleBase<SocketCommandContext>
    {
        [Command("prefix")]
        [Summary("Change the command prefix")]
        [ManageServer]
        public async Task ChangePrefix([Summary("The prefix you want to use")] string prefix)
        {
            var serverId = Context.Guild.Id.ToString();
            using (var conn = BotContext.OpenConnection())
            {
                var id =
                    await conn.QuerySingleOrDefaultAsync<int>(
                        "select \"Id\" from public.\"Servers\" where \"DiscordId\" = @discordId",
                        new {discordId = serverId});

                if (id != 0)
                {
                    await conn.ExecuteAsync("update public.\"Servers\" set \"Prefix\" = @prefix where \"Id\" = @id",
                                            new {prefix = prefix, id = id});

                    await Response.Success(Context, "Successfully updated the prefix for the server.");
                }
            }
        }

        [Command("purge", RunMode = RunMode.Async)]
        [Summary("Clear a channel of its messages")]
        [Alias("prune")]
        [ManageServer]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PurgeMessages([Summary("The number of messages to delete")] uint amount)
        {
            var messages = await Context.Channel.GetMessagesAsync((int) amount).Flatten();
            var list = messages.ToList();
            list.RemoveAll(m => m.IsPinned);
            var startMessage = await Context.Channel.SendMessageAsync("Purging channel...");
            await Context.Channel.DeleteMessagesAsync(list);
            await startMessage.DeleteAsync();
            var endMessage = await Context.Channel.SendMessageAsync("Purge completed.");
            await Task.Delay(5000);
            await endMessage.DeleteAsync();
        }
    }

    [Group("Roles")]
    [Alias("role")]
    [Summary("Manage server roles")]
    public class Roles : ModuleBase<SocketCommandContext>
    {
        [Command]
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
                int i = 1;
                foreach (var role in roles)
                {
                    description += $"{i}. {role.Name}\n";
                    i++;
                }
                response.WithDescription(description);

                await ReplyAsync("", false, response.Build());
            }
            else
            {
                await Response.Error(Context, "This server doesn't have any self-assignable roles.");
            }
        }

        [Command]
        [Alias("get")]
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
                    var contains = userRoles?.Contains(ulong.Parse(roleResult.DiscordId));
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
                                contains = userRoles?.Contains(ulong.Parse(colour.DiscordId));
                                if (!contains.GetValueOrDefault()) continue;
                                var unassign =
                                    Context.Guild.Roles.SingleOrDefault(
                                        r => String.Equals(
                                            r.Id.ToString(), colour.DiscordId,
                                            StringComparison.CurrentCultureIgnoreCase));

                                await ((IGuildUser) Context.User).RemoveRoleAsync(unassign);
                            }
                        }

                        await ((IGuildUser) Context.User).AddRoleAsync(assign);
                        await Response.Success(Context, $"Good job! You managed to get the '{assign?.Name}' role!");
                    }
                    else
                    {
                        await Response.Error(Context, "You already have this role assigned to you.");
                    }
                }
                else
                {
                    await Response.Error(Context, $"There isn't a self-assignable role called '{role}'.");
                }
            }
            else
            {
                await Response.Error(Context, "There are no self-assignable roles for the server.");
            }
        }

        [Command("remove")]
        [Summary("Removes a role from you")]
        public async Task RemoveRole([Summary("The name of the role you want to remove")] string role)
        {
            Role roleResult;
            using (var conn = BotContext.OpenConnection())
            {
                roleResult = 
                    await conn.QuerySingleOrDefaultAsync<Role>(
                        "select * from public.\"Roles\" where lower(\"Name\") = @name", new {name = role.ToLower()});
            }

            if (roleResult == null)
            {
                await Response.Error(Context, $"No role with the name {role} was found.");
                return;
            }

            var user = Context.User as IGuildUser;
            Debug.Assert(user != null, "user != null");

            var query = from r in user.RoleIds.AsParallel()
                        where r == ulong.Parse(roleResult.DiscordId)
                        select r;

            if (query.Count() != 0)
            {
                query.ForAll(async r =>
                {
                    var guildRole = Context.Guild.Roles.FirstOrDefault(x => x.Id == r);
                    await user.RemoveRoleAsync(guildRole);
                    await Response.Success(Context, $"Removed '{roleResult.Name}' from your roles.");
                });
                return;
            }
            await Response.Error(Context, $"You don't have '{roleResult.Name}' assigned to you.");
        }


        [Command("self")]
        [Summary("Make a role self-assignable")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task SetAssignable(
            [Summary("The role you want to make self-assignable")] string role,
            [Summary("Marks the role as a colour role. Enables automatic colour removal and assigning of colours for users. Example: roles self Blue -c")] string colour = "")
        {
            var serverRoles = Context.Guild.Roles.ToList();
            var roleFind = 
                serverRoles.Find(r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));

            using (var conn = BotContext.OpenConnection())
            {
                if (roleFind != null)
                {
                    var query =
                        await conn.QuerySingleOrDefaultAsync<Role>(
                            "select * from public.\"Roles\" where \"Name\" = @name", new {name = role.ToLower()});
                    if (query == null)
                    {
                        var isColour = false;
                        var serverId =
                            await conn.QuerySingleOrDefaultAsync<int>(
                                "select \"Id\" from public.\"Servers\" where \"DiscordId\" = @discordId",
                                new {discordId = Context.Guild.Id.ToString()});
                        if (colour == "-c")
                            isColour = true;

                        await conn.ExecuteAsync(
                            "insert into public.\"Roles\" (\"ServerId\", \"DiscordId\", \"Name\", \"IsColour\") " +
                            "values (@server, @discordId, @name, @colour",
                            new
                            {
                                server = serverId,
                                discordId = roleFind.Id.ToString(),
                                name = roleFind.Name,
                                colour = isColour
                            });

                        await Response.Success(Context,
                            $"Added '{roleFind.Name}' to the list of self-assignable roles.");
                    }
                    else
                    {
                        await Response.Error(Context, $"The '{roleFind.Name}' role is already set as self-assignable.");
                    }
                }
                else
                {
                    await Response.Error(Context, $"No role with the name '{role}' was found in the server.");
                }
            }
        }

        [Command("remove")]
        [Summary("Remove a role from the self-assignable list")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveAssignable([Summary("The role whose name you wish to remove")] string role)
        {
            using (var conn = BotContext.OpenConnection())
            {
                var remove = await conn.QuerySingleOrDefaultAsync<Role>(
                        "select * from public.\"Roles\" where \"Name\" = @name", new { name = role.ToLower() });

                if (remove != null)
                {
                    await conn.ExecuteAsync("delete from \"Roles\" where \"Id\" = @id", new {id = remove.Id});

                    await Response.Success(Context, $"The '{remove.Name}' role has been successfully " +
                                                    $"removed from the self-assignable list.");
                }
                else
                {
                    await Response.Error(Context,
                        $"No role with the name '{role}' has been found in the self-assignable list");
                }
            }
        }

        private async Task<List<Role>> GetServerRolesAsync()
        {
            List<Role> roles;
            using (var conn = BotContext.OpenConnection())
            {
                var serverId =
                    await conn.QueryFirstAsync<int>(
                        "select \"Id\" from public.\"Servers\" where \"DiscordId\" = @discordId",
                        new {discordId = Context.Guild.Id.ToString()});

                var sql = "select * from public.\"Roles\" " +
                          "where \"ServerId\" = @id";
                var query = await conn.QueryAsync<Role>(sql, new {id = serverId});
                roles = query.ToList();
            }

            return roles;
        }
    }
}