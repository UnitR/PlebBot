﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Services.Weather
{
    public class WeatherService
    {
        private readonly HttpClient httpClient;
        private readonly string apiAddress;
        private readonly Repository<User> userRepo;

        public WeatherService(HttpClient client, Repository<User> repo)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this.apiAddress = $"http://api.wunderground.com/api/{config["tokens:weather_key"]}";
            this.httpClient = client;
            this.userRepo = repo;
        }

        public async Task<EmbedBuilder> CurrentWeather(string location)
        {
            EmbedBuilder embed;

            if (location == null)
            {
                embed = WeatherResponse.NoLocation();
                return embed;
            }

            try
            {
                var address = $"{this.apiAddress}/conditions/forecast/q/{location}.json";
                embed = await BuildCurrentWeatherEmbed(address);
            }
            catch (RuntimeBinderException)
            {
                embed = WeatherResponse.NoInformation();
            }

            return embed;
        }

        public async Task<EmbedBuilder> Forecast(string location)
        {
            var call = $"{apiAddress}/conditions/forecast/q/{location}.json";
            var embed = await BuildForecastEmbed(call);

            return embed;
        }

        private async Task<dynamic> GetWeatherData(string address)
        {
            var json = await httpClient.GetStringAsync(address);
            dynamic response = JsonConvert.DeserializeObject(json);

            if (response.response.results == null) return response;

            address =
                address.Replace(
                    address.Substring(address.LastIndexOf("/q/", StringComparison.InvariantCulture)),
                    $"{response.response.results.First.l.ToString()}.json");

            response = await GetWeatherData(address);
            return response;
        }

        private async Task<EmbedBuilder> BuildCurrentWeatherEmbed(string address)
        {
            var response = await GetWeatherData(address);
            var observation = response.current_observation;
            var forecast = response.forecast.simpleforecast.forecastday.First;

            var kmh = (observation.wind_kph != 0) ? $"{observation.wind_kph} km/h" : "";
            var mph = (observation.wind_mph != 0) ? $"({observation.wind_mph} mph)" : "";

            string windDir = observation.wind_dir.ToString().ToLowerInvariant();
            windDir = await DetermineWind(windDir);
            var windText = 
                (windDir != String.Empty && (kmh != String.Empty || mph != String.Empty)) ? 
                $"Moving {windDir} at {kmh} {mph}" : "Calm";

            var embed = new EmbedBuilder();
            embed.WithTitle($"Current weather in {observation.display_location.full}");
            embed.WithUrl(observation.forecast_url.ToString());
            embed.WithThumbnailUrl($"https://icons.wxug.com/i/c/b/{observation.icon}.gif");
            embed.AddInlineField(
                "Weather Condition:",
                $"{observation.weather} | Feels like " +
                $"{observation.feelslike_c}°C ({observation.feelslike_f}°F) | " +
                $"Actual: {observation.temp_c}°C ({observation.temp_f}°F)\n" +
                $"High: {forecast.high.celsius}°C ({forecast.high.fahrenheit}°F) | " +
                $"Low: {forecast.low.celsius}°C ({forecast.low.fahrenheit}°F)");
            embed.AddInlineField("Wind:", windText);
            embed.AddInlineField("Humidity:", $"{observation.relative_humidity}");
            embed.WithColor(237, 126, 0);

            return embed;
        }

        private async Task<EmbedBuilder> BuildForecastEmbed(string address)
        {
            var forecast = await GetWeatherData(address);

            var embed = new EmbedBuilder();
            embed.WithTitle(
                $"Weather forecast for {forecast.current_observation.display_location.full}");
            embed.WithUrl(forecast.current_observation.forecast_url.ToString());
            embed.WithColor(237, 126, 0);

            var fct = forecast.forecast.simpleforecast.forecastday;
            var dayCount = 0;
            for (var i = 0; i < (int) fct.Last.period; i++)
            {
                var day = fct[i];

                if (!Enum.IsDefined(typeof(Days), day.date.weekday.ToString())) continue;

                string windDir = await DetermineWind(day.avewind.dir.ToString());
                embed.AddField(
                    $"{day.date.weekday} ({day.date.monthname} {day.date.day})",
                    $"Conditions: {day.conditions} | " +
                    $"High: {day.high.celsius}°C ({day.high.fahrenheit}°F) | " +
                    $"Low: {day.low.celsius}°C ({day.low.fahrenheit}°F) | " +
                    $"Wind: {windDir} at {day.avewind.kph} km/h ({day.avewind.mph} mph) | " +
                    $"Humidty: {day.avehumidity}%");

                dayCount++;
                if (dayCount == 3) break;
            }

            return embed;
        }

        private static Task<string> DetermineWind(string windDir)
        {
            Enum.TryParse(windDir, out Directions dir);
            switch (dir)
            {
                case Directions.NNW:
                case Directions.N:
                case Directions.NNE:
                    windDir = "north";
                    break;
                case Directions.NE:
                    windDir = "northeast";
                    break;
                case Directions.ENE:
                case Directions.E:
                case Directions.ESE:
                    windDir = "east";
                    break;
                case Directions.SE:
                    windDir = "southeast";
                    break;
                case Directions.SSE:
                case Directions.S:
                case Directions.SSW:
                    windDir = "south";
                    break;
                case Directions.SW:
                    windDir = "southwest";
                    break;
                case Directions.WSW:
                case Directions.W:
                case Directions.WNW:
                    windDir = "west";
                    break;
                case Directions.NW:
                    windDir = "northwest";
                    break;
                default:
                    windDir = "";
                    break;
            }

            return Task.FromResult(windDir);
        }

        public async Task<EmbedBuilder> SaveLocation(string location, long userId)
        {
            EmbedBuilder embed;
            if (location == null)
            {
                embed = WeatherResponse.NoLocation();
                return embed;
            }

            location = location.Substring(4);
            var condition = $"\"DiscordId\" = {userId}";
            var user = await this.userRepo.FindFirst(condition);
            if (user != null)
            {
                await userRepo.UpdateFirst("City", location, condition);
            }
            else
            {
                string[] columns = {"DiscordId", "City"};
                object[] values = {userId, location};
                await userRepo.Add(columns, values);
            }

            embed = WeatherResponse.SuccessfulLocationSet();
            return embed;
        }
    }

    public enum Days
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    public enum Directions
    {
        NNW,
        N,
        NNE,
        NE,
        ENE,
        E,
        ESE,
        SE,
        SSE,
        S,
        SSW,
        SW,
        WSW,
        W,
        WNW,
        NW
    }
}