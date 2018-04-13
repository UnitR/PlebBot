using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Services;

namespace PlebBot.Modules
{
    [Group("info")]
    public class Information : BaseModule
    {
        private readonly LastFmService lastFm;

        public Information(LastFmService service)
        {
            lastFm = service;
        }

        [Command("artist", RunMode = RunMode.Async)]
        public async Task SendArtistInfo([Remainder] string name)
        {
            var response = await lastFm.GetInformationAsync(Category.Artist, name);

            if (response == null)
            {
                await Error("Couldn't find any information for the given artist.");
                return;
            }

            EmbedBuilder embed = await BuildResponse(
                response.artist.image, response.artist.bio.summary, response.artist.tags.tag,
                response.artist.name.ToString(), response.artist.url, response.artist.stats.playcount, 
                response.artist.stats.listeners);

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("album", RunMode = RunMode.Async)]
        public async Task SendAlbumInfo([Remainder] string query)
        {
            var response = await lastFm.GetInformationAsync(Category.Album, query);

            if (response == null)
            {
                await Error("No information found. Correct usage is `info [artist] - [album title]`");
                return;
            }

            dynamic description = null;
            if (response.album.wiki != null) description = response.album.wiki.summary;

            EmbedBuilder embed = await BuildResponse(
                response.album.image, description, response.album.tags.tag,
                $"*{response.album.name}* by {response.album.artist}", response.album.url, response.album.playcount,
                response.album.listeners);

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("track")]
        public async Task GetTrackInfo([Remainder] string query)
        {
            var response = await lastFm.GetInformationAsync(Category.Track, query);

            if (response == null)
            {
                await Error("No information found. Correct usage is `info [artist] - [track title]`");
                return;
            }

            dynamic description = null;
            if (response.track.wiki != null) description = response.track.wiki.summary;

            var title = $"'{response.track.name}' by {response.track.artist.name}";
            dynamic images = null;
            if (response.track.album != null)
            {
                title += $" from the album {response.track.album.title}";
                images = response.track.album.image;
            }

            EmbedBuilder embed = await BuildResponse(
                images, description, response.track.toptags.tag, title,
                response.track.url, response.track.playcount, response.track.listeners);

            await ReplyAsync("", embed: embed.Build());
        }

        private async Task<EmbedBuilder> BuildResponse(
            dynamic images, dynamic description, dynamic tags, string name,
            dynamic url, dynamic playCount, dynamic listeners)
        {
            var imageUrl = "";
            if (images != null && images.Count > 0) imageUrl = await LastFmService.ChooseImage(images);

            var strDescription = "";
            if (description != null)
            {
                strDescription = description.ToString();
                strDescription =
                    !String.IsNullOrEmpty(strDescription)
                        ? strDescription.Substring(0, strDescription.IndexOf('.') + 1)
                        : "";
            }

            var tagsBuilder = new StringBuilder();
            if (tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    tagsBuilder.Append($"{new CultureInfo("en-US").TextInfo.ToTitleCase(tag.name.ToString())}, ");
                }

                tagsBuilder.Remove(tagsBuilder.Length - 2, 2);
            }

            var embed = new EmbedBuilder();
            embed.WithTitle(name);
            embed.WithUrl(url.ToString());
            embed.WithThumbnailUrl(imageUrl);
            embed.AddField("Total Scrobbles:", String.Format("{0:n0}", (int) playCount), true);
            embed.AddField("Total Listeners:", String.Format("{0:n0}", (int) listeners), true);
            if (strDescription != String.Empty && !strDescription.Contains("<a"))
                embed.WithDescription(strDescription);
            if (tagsBuilder.Length > 0) embed.AddField("Tags:", tagsBuilder.ToString());
            embed.WithColor(255, 255, 255);

            return embed;
        }
    }
}
