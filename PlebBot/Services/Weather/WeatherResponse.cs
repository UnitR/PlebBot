using Discord;

namespace PlebBot.Services.Weather
{
    public static class WeatherResponse
    {
        public static EmbedBuilder NotLinkedError()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Error",
                Description = "You haven't linked a location to your profile.",
                Color = Color.DarkRed
            };

            return embed;
        }

        public static EmbedBuilder NoInformation()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Error",
                Description = "No weather information was found for the given location.",
                Color = Color.DarkRed
            };

            return embed;
        }

        public static EmbedBuilder NoLocation()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Error",
                Description = "No location provided.",
                Color = Color.DarkRed
            };

            return embed;
        }

        public static EmbedBuilder SuccessfulLocationSet()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Success",
                Description = "Successfully set your location.",
                Color = Color.DarkGreen
            };
            return embed;
        }
    }
}