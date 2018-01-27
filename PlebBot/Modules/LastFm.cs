using System;
using System.Linq;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api;
using Microsoft.Extensions.Configuration;

namespace PlebBot.Modules
{
    [Group("fm")]
    class LastFm : ModuleBase<SocketCommandContext>
    {
        private LastfmClient _client;

        public LastFm()
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this._client = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
        }

        [Command]
        [Summary("Show what you're listening to")]
        public async Task Scrobble(string username = "")
        {
            var msg = new EmbedBuilder();

            if (username != String.Empty)
            {
                var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);
                msg.WithTitle($"Recent tracks for {username}")
                    .WithThumbnailUrl(response.Content[0].Images.Medium.ToString())
                    .WithUrl($"https://www.last.fm/user/{username}")
                    .AddField("**Current track:**",
                        $"{response.Content[0].ArtistName} - {response.Content[0].Name} " +
                        $"[{response.Content[0].AlbumName}]")
                    .AddField("**Previous:**",
                        $"{response.Content[1].ArtistName} - {response.Content[1].Name} " +
                        $"[{response.Content[1].AlbumName}]")
                    .WithColor(Color.DarkBlue);
            }
            else
            {
                msg.AddField("Error", "You must enter a username.")
                    .WithColor(Color.DarkRed);
            }

            await Context.Channel.SendMessageAsync("", false, msg);
        }
    }
}