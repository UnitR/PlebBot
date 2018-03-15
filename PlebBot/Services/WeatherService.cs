using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Services
{
    public class WeatherService
    {
        private readonly HttpClient httpClient;
        private readonly string apiAddress;
        private readonly Repository<User> userRepo;

        public WeatherService(HttpClient client, Repository<User> repo)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this.apiAddress = $"http://api.wunderground.com/api/{config["tokens:weather_key"]}/conditions/q/";
            this.httpClient = client;
            this.userRepo = repo;
        }

        public async Task<EmbedBuilder> Weather(string city, long userId)
        {
            EmbedBuilder embed;
            if (city.Contains("set "))
            {
                await SaveLocation(city, userId);

                embed = new EmbedBuilder()
                {
                    Title = "Success",
                    Description = "Successfully set your city.",
                    Color = Color.DarkGreen
                };
                return embed;
            }

            if (city == String.Empty)
            {
                var condition = $"\"DiscordId\" = {userId}";
                var user = await userRepo.FindFirst(condition);
                city = user.City;
            }

            try
            {
                var address = $"{this.apiAddress}{city}.json";
                var response = await GetWeatherData(address);
                var observation = response.current_observation;
                embed = await BuildCurrentWeatherEmbed(observation);
            }
            catch (RuntimeBinderException)
            {
                embed = new EmbedBuilder()
                {
                    Title = "Error",
                    Description = "No weather information was found for the given location.",
                    Color = Color.DarkRed
                };
            }

            return embed;
        }

        private async Task<dynamic> GetWeatherData(string address)
        {
            var json = await httpClient.GetStringAsync(address);
            dynamic response = JsonConvert.DeserializeObject(json);

            return response;
        }

        private Task<EmbedBuilder> BuildCurrentWeatherEmbed(dynamic observation)
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Current weather in {observation.display_location.full}");
            embed.WithUrl(observation.forecast_url.ToString());
            embed.WithThumbnailUrl($"https://icons.wxug.com/i/c/b/{observation.icon}.gif");
            embed.AddInlineField(
                "Weather Condition:",
                $"{observation.weather} | Feels like " +
                $"{observation.feelslike_c} Celsius ({observation.feelslike_f} Fahrenheit)");
            embed.AddInlineField(
                "Wind:",
                $"{observation.wind_string} ({observation.wind_kph} km/h)");
            embed.AddInlineField("Humidity:", $"{observation.relative_humidity}");
            embed.WithColor(237, 126, 0);

            return Task.FromResult(embed);
        }

        private async Task SaveLocation(string city, long userId)
        {
            city = city.Substring(4);
            var condition = $"\"DiscordId\" = {userId}";
            var user = await this.userRepo.FindFirst(condition);
            if (user != null)
            {
                await userRepo.UpdateFirst("City", city, condition);
            }
            else
            {
                string[] columns = {"DiscordId", "City"};
                object[] values = {userId, city};
                await userRepo.Add(columns, values);
            }
        }
    }
}