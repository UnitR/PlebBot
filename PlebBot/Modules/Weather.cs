using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.CSharp.RuntimeBinder;
using PlebBot.Services.Weather;

namespace PlebBot.Modules
{
    [Name("Weather")]
    public class Weather : BaseModule
    {
        private readonly WeatherService service;

        public Weather(WeatherService service)
        {
            this.service = service;
        }

        [Command("weather", RunMode = RunMode.Async)]
        [Alias("w")]
        [Summary("Check current weather conditions")]
        public async Task DisplayWeater(
            [Summary("The city you wish to check the current weather for")] [Remainder] string location = "")
            => await HandleRequestAsync(location);

        [Command("weather set", RunMode = RunMode.Async)]
        [Alias("wset")]
        [Summary("Link a city to your profile")]
        public async Task SetLocation(
            [Summary("The city you want to link to your profile")] [Remainder] string location)
            => await HandleRequestAsync(location);

        [Command("forecast", RunMode = RunMode.Async)]
        [Alias("fc")]
        [Summary("Get the weather forecast for 3 days")]
        public async Task GetForecast(
            [Summary("The location you want to check the forecast for")] [Remainder] string location = "")
        {
            EmbedBuilder embed;
            if (location == String.Empty)
            {
                var user = await FindUserAsync();
                if (user.City == null)
                {
                    embed = await service.NotLinkedError();
                    await ReplyAsync("", embed: embed.Build());
                    return;
                }
                location = user.City;

                try
                {
                    embed = await service.Forecast(location);
                }
                catch (RuntimeBinderException)
                {
                    embed = await service.NoInformation();
                }

                await ReplyAsync("", embed: embed);
                return;
            }

            embed = await service.Forecast(location);
            await ReplyAsync("", embed: embed.Build());
        }

        //Handle the difference between "weather set" and "weather" due to a Discord.Net limitation
        private async Task HandleRequestAsync(string location)
        {
            EmbedBuilder embed;

            if (location.Contains("set "))
            {
                location = location.Substring(4);
                if (String.IsNullOrEmpty(location))
                {
                    embed = await service.NoLocation();
                }
                else
                {
                    await SaveUserData("City", location);
                    embed = await service.SuccessfulLocationSet();
                }

                await ReplyAsync("", embed: embed.Build());
                return;
            }

            if (location == String.Empty)
            {
                var user = await FindUserAsync();
                location = user.City;
            }

            embed = await service.CurrentWeather(location);
            await ReplyAsync("", embed: embed.Build());
        }
    }
}