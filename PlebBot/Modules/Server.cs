using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Helpers;
using PlebBot.Preconditions;

namespace PlebBot.Modules
{
    [Group("s")]
    [Summary("Manage server settings")]
    [ManageServer]
    public class Admin : ModuleBase<SocketCommandContext>
    {
        private readonly BotContext _dbContext;

        public Admin(BotContext dbContext)
        {
            this._dbContext = dbContext;
        }

        //TODO: Make sure every server uses its own prefix. But does it even matter if it's only going to be used only in PMCD for a very long time if not forever?
        [Command("prefix")]
        [Summary("Change the command prefix")]
        [ManageServer]
        public async Task ChangePrefix([Summary("The prefix you want to use")] string prefix)
        {
            var serv = await _dbContext.Servers.SingleOrDefaultAsync(
                s => s.DiscordId == Context.Guild.Id.ToString());
            if (serv != null)
            {
                serv.Prefix = prefix;
                _dbContext.Update(serv);
                await _dbContext.SaveChangesAsync();
                await Response.Success(Context, "Successfully updated the prefix for the server.");
            }
        }
    }

    [Group("roles")]
    [Summary("Manage server roles")]
    public class Roles : ModuleBase<SocketCommandContext>
    {
        protected readonly BotContext _dbContext;

        public Roles(BotContext context)
        {
            this._dbContext = context;
        }

        [Group("self")]
        public class SelfRoles : Roles
        {
            public SelfRoles(BotContext context) : base(context) { }

            [Command]
            [Summary("Get a list of the self-assignable roles")]
            public async Task GetAssignable()
            {
                var roles = await _dbContext.Roles.ToListAsync();
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
            [Summary("Make a role self-assignable")]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task SetAssignable(
                [Summary("The role you want to make self-assignable")] string role,
                [Summary("Marks the role as a colour role. Enables automatic colour removal and assigning of colours for users. Example: `roles self Blue c`")] char colour = ' ')
            {
                var serverRoles = Context.Guild.Roles.ToList();
                var roleFind = serverRoles.Find(
                    r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                if (roleFind != null)
                {
                    if (_dbContext.Roles.SingleOrDefault(
                            r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase)) == null)
                    {
                        bool isColour = false;
                        var server =
                            await _dbContext.Servers.SingleOrDefaultAsync(
                                s => s.DiscordId == Context.Guild.Id.ToString());

                        if (colour == 'c')
                            isColour = true;

                        await _dbContext.Roles.AddAsync(new Role()
                        {
                            Server = server,
                            DiscordId = roleFind.Id.ToString(),
                            Name = roleFind.Name,
                            IsColour = isColour
                        });
                        await _dbContext.SaveChangesAsync();

                        await Response.Success(Context, $"Added '{roleFind.Name}' to the list of self-assignable roles.");
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

            [Command("remove")]
            [Summary("Remove a role from the self-assignable list")]
            public async Task RemoveAssignable([Summary("The role whose name you wish to remove")] string role)
            {
                var remove = await _dbContext.Roles.FirstOrDefaultAsync(
                    r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                if (remove != null)
                {
                    _dbContext.Roles.RemoveRange(remove);
                    await _dbContext.SaveChangesAsync();

                    await Response.Success(Context, $"The '{remove.Name}' role has been successfully " +
                                                    $"removed from the self-assignable list.");
                }
                else
                {
                    await Response.Error(Context,
                        $"No role with the name '{role}' has been found in the self-assignable list");
                }
            }


            [Command("get")]
            [Summary("Get a self-assignable role")]
            public async Task GetRole(string role)
            {
                var serverId = _dbContext.Servers.SingleOrDefaultAsync(
                    s => s.DiscordId == Context.Guild.Id.ToString()).Result.Id;
                var roles = await _dbContext.Roles.Where(r => r.Server.Id == serverId).ToListAsync();
                if (roles.Any())
                {
                    var roleResult = roles.Find(
                        r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                    if (roleResult != null)
                    {
                        var userRoles = (Context.User as IGuildUser).RoleIds;
                        if (!userRoles.Contains(ulong.Parse(roleResult.DiscordId)))
                        {
                            var assign = Context.Guild.Roles.SingleOrDefault(
                                r => String.Equals(r.Name, role, StringComparison.CurrentCultureIgnoreCase));
                            var colours = roles.Where(r => r.IsColour).ToList();
                            foreach (var colour in colours)
                            {
                                if (userRoles.Contains(ulong.Parse(colour.DiscordId)))
                                {
                                    var unassign =
                                        Context.Guild.Roles.SingleOrDefault(
                                            r => String.Equals(
                                                r.Id.ToString(), colour.DiscordId,
                                                StringComparison.CurrentCultureIgnoreCase));

                                    await (Context.User as IGuildUser).RemoveRoleAsync(unassign);
                                }
                            }

                            await (Context.User as IGuildUser).AddRoleAsync(assign);
                            await Response.Success(Context, $"Good job! You managed to get the '{assign.Name}' role!");
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
        }

    }
}