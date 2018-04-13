using System;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace PlebBot.Modules
{
    //TODO: Completely redesign this module
    public class Help : BaseModule
    {
        private readonly CommandService service;

        public Help(CommandService service)
        {
            this.service = service;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder
            {
                Color = Color.Green,
                Title = "Available commands:"
            };

            foreach (var module in service.Modules)
            {
                if (String.Equals(module.Name, "help", StringComparison.CurrentCultureIgnoreCase)) continue;

                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (!result.IsSuccess) continue;

                    description += $"{cmd.Aliases.First()} ";
                    if (cmd.Parameters != null)
                    {
                        var parameters = cmd.Parameters.ToList();
                        foreach (var param in parameters)
                        {
                            if (param.IsOptional)
                                description += $"({param.Name}) ";
                            else
                                description += $"[{param.Name}] ";
                        }
                    }
                    description += $"- *{cmd.Summary}*\n";
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

            builder.AddField(
                "Additional help:", 
                "For more help regarding a command use `help [command name]` without the brackets.");
            await ReplyAsync("", embed: builder.Build());
        }

        [Command("help")]
        public async Task HelpAsync(params string[] command)
        {
            var cmd = command.Aggregate("", (current, item) => current + $"{item} ");
            cmd = cmd.Remove(cmd.Length - 1);

            var builder = new EmbedBuilder();
            var result = service.Search(Context, cmd);

            if (!result.IsSuccess)
            {
                await Error($"\"Sorry, I couldn\'t find a command like `{cmd}`");
                return;
            }

            builder.Color = Color.Green;
            foreach (var match in result.Commands)
            {
                if (match.Command.Name != cmd && !match.Command.Aliases.Contains(cmd)) continue;

                var matched = match.Command;
                var parameters = "";

                if (matched.Parameters.Count > 0)
                {
                    parameters = matched.Parameters.Aggregate("Parameters:\n", (current, item) 
                        => current + $"\t{item.Name} - *{item.Summary}*\n");
                }

                builder.AddField(x =>
                {
                    x.Name = $"{matched.Aliases.First()}";
                    x.Value = parameters +
                              "Summary:\n" +
                              $"\t{matched.Summary}.\n\n\n";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", embed: builder.Build());
        }
    }
}