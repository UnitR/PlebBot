using System;
using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;
using PlebBot.Services.LastFm;
using PlebBot.Preconditions;

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

        [Priority(0)]
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
            => await lastFm.SaveUserAsync(username, Context.User.Id);

        [Chartposting]
        [Priority(1)]
        [Command("fm top artists", RunMode = RunMode.Async)]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of artists to show. Maximum 25. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ChartType.Artists, limit, span, username);

        [Priority(2)]
        [Chartposting]
        [Command("fm top artists", RunMode = RunMode.Async)]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists([Summary("Number of artists to show. Maximum 25. Default is 10.")] int limit = 10)
            => await SendChartAsync(ChartType.Artists, limit);

        [Chartposting]
        [Command("fm top albums", RunMode = RunMode.Async)]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of albums to show. Maximum 50. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ChartType.Albums, limit, span, username);

        [Priority(1)]
        [Chartposting]
        [Command("fm top albums", RunMode = RunMode.Async)]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums([Summary("Number of albums to show. Maximum 50. Default is 10.")] int limit = 10)
            => await SendChartAsync(ChartType.Albums, limit);

        [Chartposting]
        [Command("fm top tracks", RunMode = RunMode.Async)]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of tracks to show. Maximum 50. Default is 10.")] int limit = 10,
            [Summary("Your last.fm username")] string username = "")
            => await SendChartAsync(ChartType.Tracks, limit, span, username);

        [Chartposting]
        [Priority(1)]
        [Command("fm top tracks", RunMode = RunMode.Async)]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks([Summary("Number of tracks to show. Maximum 50. Default is 10.")] int limit = 10)
            => await SendChartAsync(ChartType.Tracks, limit);

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

        private async Task SendChartAsync(ChartType chartType, int limit, string span = "", string username = "")
        {
            if (username == String.Empty)
            {
                var user = await FindUserAsync();
                if (user.LastFm != null) username = user.LastFm;
                else
                {
                    await Error("You haven't linked your last.fm account.");
                    return;
                }
            }
            var response = await lastFm.GetChartAsync(chartType, limit, span, username, Context.User.Id);
            await ReplyAsync("", embed: response.Build());
        }

        private async Task<string> GetUsername(ulong userId)
        {
            var condition = $"\"DiscordId\" = {(long) userId}";
            var user = await userRepo.FindFirst(condition);
            return user.LastFm;
        }
    }
}