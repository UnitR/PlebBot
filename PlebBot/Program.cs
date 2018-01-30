using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PlebBot.Modules;
using PlebBot.Data;
using PlebBot.Data.Migrations;
using PlebBot.Data.Models;
using Roles = PlebBot.Modules.Roles;

namespace PlebBot
{
    public class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private IConfigurationRoot _config;
        private BotContext _context;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("_config.json");
            _config = builder.Build();

            _services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Verbose
                }))
                .AddSingleton<Random>()
                .AddSingleton(_config)
                .AddEntityFrameworkNpgsql()
                .AddDbContext<BotContext>(options => options.UseNpgsql(_config["connection_string"]))
                .BuildServiceProvider();

            _context = _services.GetService<BotContext>();

            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.JoinedGuild += JoinGuild;
            _client.LeftGuild += LeaveGuild;

            _commands = new CommandService();
            await InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, _config["tokens:discord_token"]);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModuleAsync<Miscellaneous>();
            await _commands.AddModuleAsync<LastFm>();
            await _commands.AddModuleAsync<Help>();
            await _commands.AddModulesAsync(Assembly.GetAssembly(typeof(Admin)));
            await _commands.AddModulesAsync(Assembly.GetAssembly(typeof(Roles)));
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            var context = new SocketCommandContext(_client, message);

            var server = await _context.Servers.SingleOrDefaultAsync(s => s.DiscordId == context.Guild.Id.ToString());
            var prefix = server.Prefix;

            if (!(message.HasStringPrefix(prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot) return;
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess && result.ErrorReason != "Unknown command.")
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async Task JoinGuild(SocketGuild guild)
        {
            if (guild == null) return;

            _context.Servers.Add(new Data.Models.Server()
            {
                DiscordId = guild.Id.ToString()
            });
            await _context.SaveChangesAsync();
        }

        private async Task LeaveGuild(SocketGuild guild)
        {
            if (guild == null) return;

            var server = await _context.Servers.SingleOrDefaultAsync(s => s.DiscordId == guild.Id.ToString());
            if (server != null)
            {
                _context.Servers.Remove(server);
                await _context.SaveChangesAsync();
            }
        }
    }
}