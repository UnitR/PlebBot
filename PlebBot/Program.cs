using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PlebBot.Modules;
using PlebBot.Data;

namespace PlebBot
{
    public partial class Program
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
            await _client.SetGameAsync("top 40 hits");

            _client.Log += Log;
            _client.JoinedGuild += HandleJoinGuildAsync;
            _client.LeftGuild += HandleLeaveGuildAsync;
            //_client.MessageDeleted += HandleMessageDeletedAsync;

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
            await _commands.AddModuleAsync<Roles>();
            await _commands.AddModuleAsync<Admin>();
            await _commands.AddModuleAsync<Help>();
        }
    }
}