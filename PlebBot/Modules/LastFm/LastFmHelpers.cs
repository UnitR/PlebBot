using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using IF.Lastfm.Core.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PlebBot.Data.Models;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    partial class LastFm
    {
        private async Task SendYtLink(string username)
        {
            var scrobble = await _client.User.GetRecentScrobbles(username, null, 1, 1);
            var track = scrobble.Content[0];

            var ytSearchRequest = _ytClient.Search.List("snippet");
            ytSearchRequest.Q = $"{track.ArtistName} {track.Name}";
            ytSearchRequest.MaxResults = 1;
            ytSearchRequest.Type = "video";

            var searchResponse = await ytSearchRequest.ExecuteAsync();
            var video = searchResponse.Items.FirstOrDefault();
            var videoLink = $"https://youtu.be/{video.Id.VideoId}";

            var current = $"Current track for **{username}**:\n" +
                          $"{track.ArtistName} - *{track.Name}*";
            var currAlbum = track.AlbumName ?? "";
            if (currAlbum.Length > 0) current += $" [{currAlbum}]";

            var response = $"{current}\n" + videoLink;
            await ReplyAsync(response);
        }

        private async Task GetTopAlbumsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var albums = await _client.User.GetTopAlbums(username, span, 1, limit);
            if (albums.TotalItems > 0)
            {
                var list = "";
                var i = 1;

                foreach (var album in albums)
                {
                    list += $"{i}. {album.ArtistName} - *{album.Name}* [{album.PlayCount} scrobbles]\n";
                    i++;
                }

                await BuildTopAsync(list, username, "albums", span);
                return;
            }
            await Response.Error(Context, $"{username} hasn't scrobbled any albums.");
        }

        private async Task GetTopArtistsAsync(string username, LastStatsTimeSpan span, int limit)
        {
            var artists = await _client.User.GetTopArtists(username, span, 1, limit);
            var list = "";
            var i = 1;
            foreach (var artist in artists)
            {
                list += $"{i}. {artist.Name} [{artist.PlayCount} scrobbles]\n";
                i++;
            }

            await BuildTopAsync(list, username, "artists", span);
        }

        //send the embed with the top tracks
        private async Task SendTopTracks(string span, string username, int limit)
        {
            var timeSpan = "overall";
            switch (span.ToLower())
            {
                case "week":
                    timeSpan = "7day";
                    break;
                case "month":
                    timeSpan = "1month";
                    break;
                case "3months":
                    timeSpan = "3month";
                    break;
                case "6months":
                    timeSpan = "6month";
                    break;
                case "year":
                    timeSpan = "12month";
                    break;
            }
            var list = await GetTopTracksAsync(username, timeSpan, limit);
            var time = DetermineSpan(span);
            await BuildTopAsync(list, username, "tracks", time);
        }

        private async Task<string> GetTopTracksAsync(string username, string span, int limit)
        {
            var url =
                $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={username}&period={span}" +
                $"&limit={limit}&api_key={_lastFmKey}&format=json";
            string json;
            using (WebClient wc = new WebClient())
            {
                json = await wc.DownloadStringTaskAsync(url);
            }
            dynamic response = JsonConvert.DeserializeObject(json);
            var list = "";
            for (int i = 0; i < limit; i++)
            {
                dynamic track = response.toptracks.track[i];
                list += $"{i + 1}. {track.artist.name} - *{track.name}* [{track.playcount} scrobbles]\n";
            }

            return list;
        }

        //determines the time span used for the chart
        private LastStatsTimeSpan DetermineSpan(string span)
        {
            LastStatsTimeSpan timeSpan = LastStatsTimeSpan.Overall;
            switch (span.ToLower())
            {
                case "week":
                    timeSpan = LastStatsTimeSpan.Week;
                    break;
                case "month":
                    timeSpan = LastStatsTimeSpan.Month;
                    break;
                case "3months":
                    timeSpan = LastStatsTimeSpan.Quarter;
                    break;
                case "6months":
                    timeSpan = LastStatsTimeSpan.Half;
                    break;
                case "year":
                    timeSpan = LastStatsTimeSpan.Year;
                    break;
            }

            return timeSpan;
        }

        //builds the embed for the chart
        private async Task BuildTopAsync(string list, string username, string chartType, LastStatsTimeSpan span)
        {
            var response = new EmbedBuilder()
                .WithTitle($"Top {chartType} for {username} - {span}")
                .WithDescription(list)
                .WithColor(Color.Gold)
                .Build();
            await ReplyAsync("", false, response);
        }

        //Checks if the last.fm user exists
        private async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var response = await _client.User.GetInfoAsync(username);
            if (response.Success)
            {
                if (response.Content.Id != "")
                {
                    if (response.Content.Playcount > 0)
                        return true;

                    await Response.Error(Context, "The user hasn't scrobbled any tracks.");
                    return false;
                }
            }
            await Response.Error(Context, LastFmError.NotFound);
            return false;
        }

        //TODO: last.fm user profile picture
        //show user's scrobbles
        private async Task NowPlayingAsync(string username)
        {
            try
            {
                var response = await _client.User.GetRecentScrobbles(username, null, 1, 2);
                var tracks = response.Content;

                string currAlbum = tracks[0].AlbumName ?? "";
                string prevAlbum = tracks[1].AlbumName ?? "";
                string albumArt = (tracks[0].Images.Large != null) ? tracks[0].Images.Largest.ToString() : "";

                var msg = new EmbedBuilder();
                var currField = $"{response.Content[0].ArtistName} - {response.Content[0].Name}";
                var prevField = $"{response.Content[1].ArtistName} - {response.Content[1].Name}";
                if (currAlbum.Length > 0) currField += $" [{currAlbum}]";
                if (prevAlbum.Length > 0) prevField += $" [{prevAlbum}]";
                msg.WithTitle($"Recent tracks for {username}")
                    .WithThumbnailUrl(albumArt)
                    .WithUrl($"https://www.last.fm/user/{username}")
                    .AddField("**Current:**", currField)
                    .AddField("**Previous:**", prevField)
                    .WithColor(Color.DarkBlue);

                await ReplyAsync("", false, msg.Build());
            }
            catch (Exception ex)
            {
                await Response.Error(Context, ex.Message);
            }
        }

        //find a user in the database
        private async Task<User> DbFindUserAsync()
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(
                u => u.DiscordId == Context.User.Id.ToString());

            return user;
        }
    }
}