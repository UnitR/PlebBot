using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;
using PlebBot.Services;
using PlebBot.Services.Chart;

namespace PlebBot.Modules
{
    public class LastFm : BaseModule
    {
        private readonly LastFmService lastFm;
        private readonly Repository<User> userRepo;

        public LastFm(LastFmService service, Repository<User> repo)
        {
            lastFm = service;
            userRepo = repo;
        }

        [Command("fm", RunMode = RunMode.Async)]
        [Summary("Show what you're listening to")]
        public async Task Scrobble([Summary("Your last.fm username")] string username = "")
        {
            if (username == String.Empty)
            {
                username = await GetUsername(Context.User.Id);
                if (username == null)
                {
                    await Error("User not found.");
                    return;
                }
            }
            var response = await lastFm.NowPlayingAsync(username);
            await ReplyAsync("", embed: response.Build());
        }

        [Command("fm set", RunMode = RunMode.Async)]
        [Summary("Link your last.fm username to your profile")]
        public async Task SaveUser([Summary("Your last.fm username")] string username)
        {
            await SaveUserData("LastFm", username);
            await Success("last.fm username saved.");
        } 

        [Command("fm top artists", RunMode = RunMode.Async)]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of artists to show. Maximum 25. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ListType.Artists, limit, span, username);

        [Priority(1)]
        [Command("fm top artists", RunMode = RunMode.Async)]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists([Summary("Number of artists to show. Maximum 25. Default is 10.")] int limit = 10)
            => await SendChartAsync(ListType.Artists, limit);

        [Command("fm top albums", RunMode = RunMode.Async)]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of albums to show. Maximum 50. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ListType.Albums, limit, span, username);

        [Priority(1)]
        [Command("fm top albums", RunMode = RunMode.Async)]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums([Summary("Number of albums to show. Maximum 50. Default is 10.")] int limit = 10)
            => await SendChartAsync(ListType.Albums, limit);

        [Command("fm top tracks", RunMode = RunMode.Async)]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of tracks to show. Maximum 50. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ListType.Tracks, limit, span, username);

        [Priority(1)]
        [Command("fm top tracks", RunMode = RunMode.Async)]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks([Summary("Number of tracks to show. Maximum 50. Default is 10.")] int limit = 10)
            => await SendChartAsync(ListType.Tracks, limit);

        [Command("fmyt", RunMode = RunMode.Async)]
        [Summary("Send a YouTube link to your current scrobble")]
        public async Task YtLink([Summary("Your last.fm username")] string username = "")
        {
            if (username == String.Empty)
            {
                username = await GetUsername(Context.User.Id);
                if (username == null)
                {
                    await Error("User not found.");
                    return;
                }
            }

            var response = await lastFm.GetVideoLinkAsync(username);
            if (response == null)
            {
                await Error("No videos found.");
                return;
            }
            await ReplyAsync(response);
        }

        private async Task SendChartAsync(ListType ListType, int limit, string span = "", string username = "")
        {
            if (!await Preconditions.Preconditions.InChartposting(Context)) return;

            if (username == string.Empty)
            {
                var user = await FindUserAsync();
                if (user.LastFm != null) username = user.LastFm;
                else
                {
                    await Error("You haven't linked your last.fm account.");
                    return;
                }
            }

            var response = await lastFm.GetTopAsync(ListType, limit, span, username);
            if (response == null)
            {
                await Error("No scrobbled albums.");
                return;
            }

            var list = "";
            var i = 1;
            switch (ListType)
            {
                case ListType.Albums:
                    foreach (var album in response.topalbums.album)
                    {
                        list += $"{i}. {album.artist.name} - *{album.name}* " +
                                $"[{String.Format("{0:n0}", (int) album.playcount)} scrobbles]\n";
                        i++;
                    }
                    break;
                case ListType.Artists:
                    foreach (var artist in response.topartists.artist)
                    {
                        list += $"{i}. {artist.name} [{String.Format("{0:n0}", (int) artist.playcount)} " +
                                "scrobbles]\n";
                        i++;
                    }
                    break;
                case ListType.Tracks:
                    foreach(var track in response.toptracks.track)
                    {
                        list += $"{i}. {track.artist.name} - *{track.name}* " +
                                $"[{String.Format("{0:n0}", (int) track.playcount)} scrobbles]\n";
                        i++;
                    }
                    break;
            }
            var embed = await BuildTopAsync(list, username, ListType.ToString().ToLowerInvariant(), span);
            await ReplyAsync("", embed: embed.Build());
        }

        //builds the embed for the chart
        private async Task<EmbedBuilder> BuildTopAsync(string list, string username, string ListType, string span)
        {
            var totalScrobbles = await lastFm.TotalScrobblesAsync(span, username);
            span = await LastFmService.FormatSpan(span.ToLowerInvariant());
            var embed = new EmbedBuilder()
                .WithTitle($"Top {ListType} for {username} - {span} {totalScrobbles}")
                .WithDescription(list)
                .WithColor(Color.Gold);
            return embed;
        }

        private async Task<string> GetUsername(ulong userId)
        {
            var user = await userRepo.FindByDiscordId((long) userId);
            return user?.LastFm;
        }
    }
}