using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Services;

namespace PlebBot.Modules
{
    public class Weather : BaseModule
    {
        private readonly WeatherService service;

        public Weather(WeatherService service)
        {
            this.service = service;
        }

        [Command("weather")]
        [Alias("w")]
        [Summary("Check current weather conditions")]
        public async Task DisplayWeater(
            [Summary("The city you wish to check the current weather for")] [Remainder] string city = "")
            => await this.HandleRequestAsync(city);

        [Command("weather set", RunMode = RunMode.Async)]
        [Alias("wset")]
        [Summary("Link a city to your profile")]
        public async Task SetLocation([Summary("The city you wish to link to your profile")] [Remainder] string city)
            => await this.HandleRequestAsync(city);


        private async Task HandleRequestAsync(string city)
        {
            var embed = await service.Weather(city, (long) Context.User.Id);
            await ReplyAsync("", embed: embed.Build());
        }
    }
}