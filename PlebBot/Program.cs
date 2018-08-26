using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PlebBot.Caches.CommandCache;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;
using PlebBot.Services;
using PlebBot.Services.Chart;
using PlebBot.Services.Weather;
using PlebBot.TypeReaders;

namespace PlebBot
{
    public partial class Program
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceCollection services;
        private IConfigurationRoot config;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("_config.json");
            config = builder.Build();

            services = new ServiceCollection();
            client = new DiscordSocketClient().UseCommandCache(services, 200);

            commands = new CommandService();
            await InstallAsync();

            await client.LoginAsync(TokenType.Bot, config["tokens:discord_token"]);
            await client.SetGameAsync("top 40 hits");
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task InstallAsync()
        {
            client.Log += Log;
            client.JoinedGuild += HandleJoinGuildAsync;
            client.LeftGuild += HandleLeaveGuildAsync;
            client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private IServiceProvider ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(client);
            serviceCollection.AddSingleton(new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            })); 
            serviceCollection.AddSingleton<HttpClient>();
            serviceCollection.AddTransient<Repository<Server>>();
            serviceCollection.AddTransient<Repository<Role>>();
            serviceCollection.AddTransient<Repository<User>>();
            serviceCollection.AddSingleton(config);
            serviceCollection.AddTransient<WeatherService>();
            serviceCollection.AddTransient<YtService>();
            serviceCollection.AddTransient<LastFmService>();
            serviceCollection.AddTransient<ChartService>();
            commands.AddTypeReader<ChartSize>(new ChartSizeReader());
            commands.AddTypeReader<ChartType>(new ChartTypeReader());
            commands.AddTypeReader<ListType>(new ListTypeReader());

            return serviceCollection.BuildServiceProvider();
        }
    }
}