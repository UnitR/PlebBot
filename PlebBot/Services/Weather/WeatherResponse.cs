using System.Threading.Tasks;
using Discord;

namespace PlebBot.Services.Weather
{
    public partial class WeatherService
    {
        public Task<EmbedBuilder> NotLinkedError()
        {
            var response = new EmbedBuilder()
            {
                Title = "Error",
                Description = "You haven't linked a location to your profile.",
                Color = Color.DarkRed
            };

            return Task.FromResult(response);
        }

        public Task<EmbedBuilder> NoInformation()
        {
            var response = new EmbedBuilder()
            {
                Title = "Error",
                Description = "No weather information was found for the given location.",
                Color = Color.DarkRed
            };

            return Task.FromResult(response);
        }

        public Task<EmbedBuilder> NoLocation()
        {
            var response = new EmbedBuilder()
            {
                Title = "Error",
                Description = "No location provided.",
                Color = Color.DarkRed
            };

            return Task.FromResult(response);
        }

        public Task<EmbedBuilder> SuccessfulLocationSet()
        {
            var response = new EmbedBuilder()
            {
                Title = "Success",
                Description = "Successfully set your location.",
                Color = Color.DarkGreen
            };
            
            return Task.FromResult(response);
        }
    }
}