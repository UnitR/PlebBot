using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlebBot.Data;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly BotContext _dbContext;

        public Help(CommandService service, BotContext dbContext)
        {
            _service = service;
            this._dbContext = dbContext;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.Green,
                Title = "Available commands:"
            };

            foreach (var module in _service.Modules)
            {
                if (!String.Equals(module.Name, "help", StringComparison.CurrentCultureIgnoreCase))
                {
                    string description = null;
                    foreach (var cmd in module.Commands)
                    {
                        var result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                        {
                            description += $"{cmd.Aliases.First()} ";
                            if (cmd.Parameters != null)
                            {
                                var parameters = cmd.Parameters.ToList();
                                foreach (var param in parameters)
                                {
                                    if (param.IsOptional)
                                        description += $"({param.Name})";
                                    else
                                        description += $"[{param.Name}]";
                                }
                            }
                            description += $" - *{cmd.Summary}*\n";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(description) && module.Name != "Help")
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false;
                        });
                    }
                }
            }

            builder.AddField(
                "Additional help:", 
                "For more help regarding a command use `help [command name]` without the brackets.");
            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(params string[] command)
        {
            string cmd = "";
            foreach (var item in command)
            {
                cmd += $"{item} ";
            }
            cmd = cmd.Remove(cmd.Length - 1);

            var builder = new EmbedBuilder();
            var result = _service.Search(Context, cmd);

            if (!result.IsSuccess)
            {
                await Response.Error(Context, $"\"Sorry, I couldn\'t find a command like `{cmd}`");
                return;
            }

            builder.Color = Color.Green;
            foreach (var match in result.Commands)
            {
                if (match.Command.Name == cmd || match.Command.Aliases.Contains(cmd))
                {
                    var matched = match.Command;
                    var parameters = "";

                    if (matched.Parameters.Count > 0)
                    {
                        parameters = "Parameters:\n";
                        foreach (var item in matched.Parameters)
                        {
                            parameters += $"\t{item.Name} - *{item.Summary}*\n";
                        }
                    }

                    builder.AddField(x =>
                    {
                        x.Name = $"{matched.Aliases.First()}";
                        x.Value = parameters +
                                  $"Summary:\n" +
                                  $"\t{matched.Summary}.\n\n\n";
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}