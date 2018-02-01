using System;
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
                            description += $"{cmd.Aliases.First()} - " +
                                           $"*{cmd.Summary}*\n";
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
                else
                {
                    builder.WithFooter($"For more information on a command use `help <command>`");
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var builder = new EmbedBuilder();
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await Response.Error(Context, $"\"Sorry, I couldn\'t find a command like `{command}`");
                return;
            }

            builder.Color = Color.Green;
            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = $"{string.Join(", ", cmd.Aliases)}";
                    x.Value = $"\nParameters:\n" +
                              $"\t{string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n\n" +
                              $"Summary:\n" +
                              $"\t{cmd.Summary}.";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}