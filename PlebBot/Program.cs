using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;
using PlebBot.Helpers;
using PlebBot.Helpers.CommandCache;

namespace PlebBot
{
    public partial class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _provider;
        private IServiceCollection _services;
        private IConfigurationRoot _config;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("_config.json");
            _config = builder.Build();

            _services = new ServiceCollection();
            _client = new DiscordSocketClient().UseCommandCache(_services, 200);
            _provider = ConfigureServices(_services);

            _commands = new CommandService();
            await InstallAsync();

            await _client.LoginAsync(TokenType.Bot, _config["tokens:discord_token"]);
            await _client.SetGameAsync("top 40 hits");
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task InstallAsync()
        {
            _client.Log += Log;
            _client.JoinedGuild += HandleJoinGuildAsync;
            _client.LeftGuild += HandleLeaveGuildAsync;
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_client);
            services.AddSingleton(new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Verbose
            })); 
            services.AddSingleton<HttpClient>();
            services.AddTransient<Repository<Server>>();
            services.AddTransient<Repository<Role>>();
            services.AddTransient<Repository<User>>();
            services.AddSingleton(_config);

            return services.BuildServiceProvider();
        }
    }
}