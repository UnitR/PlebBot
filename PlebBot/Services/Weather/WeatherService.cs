using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace PlebBot.Services.Weather
{
    public partial class WeatherService
    {
        private readonly HttpClient httpClient;
        private readonly string apiAddress;
        private EmbedBuilder embed;

        public WeatherService(HttpClient client)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            apiAddress = $"http://api.wunderground.com/api/{config["tokens:weather_key"]}";
            httpClient = client;
            embed = new EmbedBuilder().WithFooter(
                "Weather data provided by the Weather Underground",
                "https://icons.wxug.com/logos/PNG/wundergroundLogo_4c_rev.png");
        }

        public async Task<EmbedBuilder> CurrentWeather(string location)
        {
            if (location == null)
            {
                embed = await NoLocation();
                return embed;
            }

            try
            {
                var address = $"{apiAddress}/conditions/forecast/q/{location}.json";
                embed = await BuildCurrentWeatherEmbed(address);
            }
            catch (RuntimeBinderException)
            {
                embed = await NoInformation();
            }

            return embed;
        }

        public async Task<EmbedBuilder> Forecast(string location)
        {
            var call = $"{apiAddress}/conditions/forecast/q/{location}.json";
            embed = await BuildForecastEmbed(call);

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

            var kmh = observation.wind_kph != 0 ? $"{observation.wind_kph} km/h" : "";
            var mph = observation.wind_mph != 0 ? $"({observation.wind_mph} mph)" : "";

            string windDir = observation.wind_dir.ToString();
            windDir = await DetermineWind(windDir);
            var windText = 
                windDir != String.Empty && (kmh != String.Empty || mph != String.Empty) ? 
                $"{windDir} at {kmh} {mph}" : "Calm";

            string iconUrl = observation.icon_url.ToString();
            iconUrl = "https://icons.wxug.com/i/c/b" + iconUrl.Substring(iconUrl.LastIndexOf('/'));

            embed.WithTitle($"Current weather in {observation.display_location.full}");
            embed.WithUrl(observation.forecast_url.ToString());
            embed.WithThumbnailUrl(iconUrl);
            embed.AddInlineField(
                "Weather Condition:",
                $"{observation.weather} |  " +
                $"Actual: {observation.temp_c}°C ({observation.temp_f}°F)\n" +
                $"Feels like: {observation.feelslike_c}°C ({observation.feelslike_f}°F) | " +
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
                    $"{day.conditions} | " +
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
                    windDir = "North";
                    break;
                case Directions.NE:
                    windDir = "Northeast";
                    break;
                case Directions.ENE:
                case Directions.E:
                case Directions.ESE:
                    windDir = "East";
                    break;
                case Directions.SE:
                    windDir = "Southeast";
                    break;
                case Directions.SSE:
                case Directions.S:
                case Directions.SSW:
                    windDir = "South";
                    break;
                case Directions.SW:
                    windDir = "Southwest";
                    break;
                case Directions.WSW:
                case Directions.W:
                case Directions.WNW:
                    windDir = "West";
                    break;
                case Directions.NW:
                    windDir = "Northwest";
                    break;
                default:
                    windDir = "";
                    break;
            }

            return Task.FromResult(windDir);
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