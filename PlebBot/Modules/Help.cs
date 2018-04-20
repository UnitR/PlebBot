using System;
using System.Collections.Generic;
using Discord.Commands;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlebBot.Modules
{
    [Group("Help")]
    public class Help : BaseModule
    {
        private readonly CommandService commnadService;

        public Help(CommandService service)
        {
            commnadService = service;
        }

        [Command(RunMode = RunMode.Async)]
        public async Task SendHelp()
        {
            var channel = await Context.User.GetOrCreateDMChannelAsync();
            var text = await BuildHelpText();
            if (text is string[] segments)
                foreach (var segment in segments)
                    await channel.SendMessageAsync(segment);
            else
            {
                string helpText = text.ToString();
                await channel.SendMessageAsync(helpText);
            }

            await ReplyAsync(
                ":mailbox: | Bot help has been sent to your DMs. If you need further help with a command, use `help command`, where command is the command name.");
        }

        [Command(RunMode = RunMode.Async)]
        public async Task CommandHelp([Remainder] string commandName)
        {
            var commandSearch = commnadService.Search(Context, commandName);

            if (!commandSearch.IsSuccess || commandSearch.Commands
                    .FirstOrDefault(c => c.Command.Name == commandName || c.Alias == commandName).Command == null)
            {
                await Error($"No command with the name '{commandName}' was found.");
                return;
            }

            var helpBuilder = new StringBuilder();
            foreach (var match in commandSearch.Commands)
            {
                if(!match.Command.Name.Contains(commandName)) continue;
                var decription = await CommandDetails(match.Command);
                helpBuilder.AppendLine(decription);
            }

            await ReplyAsync(helpBuilder.ToString());
        }

        private async Task<dynamic> BuildHelpText()
        {
            var textBuilder = new StringBuilder();
            var submoduleText = "";

            var modules = commnadService.Modules.Where(m => m.Name != "Help" && m.Name != "BaseModule");
            foreach (var module in modules)
            {
                textBuilder.AppendLine($"```{module.Name}``````");

                var shownCommands = new List<string>(module.Commands.Count);
                foreach (var command in module.Commands)
                {
                    var commandName = command.Name.ToLowerInvariant();
                    if (shownCommands.Contains(commandName)) continue;
                    textBuilder.AppendLine($"{commandName} - {command.Summary}");
                    shownCommands.Add(commandName);
                }

                if (module.Submodules.Any())
                    foreach (var submodule in module.Submodules)
                    foreach (var command in submodule.Commands)
                        submoduleText = $"{module.Name.ToLowerInvariant()} " +
                                        $"{command.Name.ToLowerInvariant()} - {command.Summary}";

                if (submoduleText != String.Empty) textBuilder.AppendLine(submoduleText);
                textBuilder.AppendLine("```\n");
                submoduleText = "";
            }
            textBuilder.AppendLine("**For information regarding a speficic command use `help command_name`**");

            var text = textBuilder.ToString();
            if (text.Length < 2000) return text;
            var segments = await SegmentText(text);
            return segments;
        }

        private static Task<string[]> SegmentText(string text)
        {
            var segments = new List<string>();
            var length = text.Length;
            while (length > 0)
            {
                var lim = length >= 2000 ? 2000 : text.Length - 1;
                segments.Add(text.Substring(0, lim));
                text = text.Remove(0, lim);
                length -= lim;
            }

            return Task.FromResult(segments.ToArray());
        }

        private static Task<string> CommandDetails(CommandInfo command)
        {
            var text = new StringBuilder();
            text.AppendLine($"`{command.Name.ToLowerInvariant()}` - *{command.Summary}*");

            if (command.Parameters.Any())
            {
                text.AppendLine("Parameters:");
                foreach (var param in command.Parameters)
                {
                    var type = param.IsOptional ? "(optional)" : "(required)";
                    text.AppendLine($"\t{type} {param.Name} - *{param.Summary}*");
                }
            }
            
            return Task.FromResult(text.ToString());
        }
    }
}
