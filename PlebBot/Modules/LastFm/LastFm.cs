using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using IF.Lastfm.Core.Api;
using Microsoft.Extensions.Configuration;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Modules
{
    public partial class LastFm : BaseModule
    {
        private readonly LastfmClient fmClient;
        private readonly string lastFmKey;
        private readonly Repository<User> userRepo;
        private readonly HttpClient httpClient;

        public LastFm(Repository<User> repo, HttpClient client)
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            this.fmClient = new LastfmClient(config["tokens:lastfm_key"], config["tokens:lastfm_secret"]);
            this.lastFmKey = config["tokens:lastfm_key"];
            this.userRepo = repo;
            this.httpClient = client;
        }

        [Command("fm", RunMode = RunMode.Async)]
        [Summary("Show what you're listening to")]
        public async Task Scrobble([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    await NowPlayingAsync(username);
                }
            }
            else
            {
                var user = await this.FindUserAsync();
                if (user != null)
                {
                    await NowPlayingAsync(user.LastFm);
                }
                else
                {
                    await this.Error(LastFmError.NotLinked);
                }
            }
        }

        [Command("fm set", RunMode = RunMode.Async)]
        [Summary("Link your last.fm username to your profile")]
        public async Task SaveUser([Summary("Your last.fm username")] string username)
        {
            if (username != null)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    var findCondition = $"\"DiscordId\" = {Context.User.Id}";
                    var user = await userRepo.FindFirst(findCondition);

                    if (user != null)
                    {
                        var updateCondition = $"\"Id\" = {user.Id}";
                        await userRepo.UpdateFirst("LastFm", username, updateCondition);

                        await this.Success("Succesfully updated your last.fm username.");
                    }
                    else
                    {
                        string[] columns = {"DiscordId", "LastFm"};
                        object[] values = {(long) Context.User.Id, username};
                        await userRepo.Add(columns, values);

                        await this.Success("last.fm username saved. You can now freely use the `fm` commands.");
                    }
                }
            }
            else
            {
                await this.Error("You must provide a username.");
            }
        }

        [Command("fm top artists", RunMode = RunMode.Async)]
        [Summary("Get the top artists for a user")]
        public async Task TopArtists(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of artists to show. Maximum 25. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopArtistsAsync(username, timeSpan, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                }
            }
            else
            {
                var user = await this.FindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopArtistsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                    return;
                }
                await this.Error(LastFmError.NotLinked);
            }
        }

        [Command("fm top albums", RunMode = RunMode.Async)]
        [Summary("Get the top albums for a user")]
        public async Task TopAlbums(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of albums to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopAlbumsAsync(username, timeSpan, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                }
            }
            else
            {
                var user = await this.FindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        var timeSpan = await DetermineSpan(span);
                        await GetTopAlbumsAsync(user.LastFm, timeSpan, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                    return;
                }
                await this.Error(LastFmError.NotLinked);
            }
        }

        [Command("fm top tracks", RunMode = RunMode.Async)]
        [Summary("Get the top tracks for a user")]
        public async Task TopTracks(
            [Summary("Time span: week, month, year, overall. Default is overall")] string span = "",
            [Summary("Number of tracks to show. Maximum 50. Default is 10.")] string limit = "10",
            [Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await TopTracksAsync(span, username, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                }
            }
            else
            {
                var user = await this.FindUserAsync();
                if (user != null)
                {
                    if (int.TryParse(limit, out int lim) && lim <= 25 && lim >= 1)
                    {
                        await TopTracksAsync(span, user.LastFm, lim);
                        return;
                    }
                    await this.Error(LastFmError.Limit);
                    return;
                }
                await this.Error(LastFmError.NotLinked);
            }
        }

        [Command("fmyt", RunMode = RunMode.Async)]
        [Summary("Send a YtService link to your current scrobble")]
        public async Task YtLink([Summary("Your last.fm username")] string username = "")
        {
            if (username != String.Empty)
            {
                if (await CheckIfUserExistsAsync(username))
                {
                    await SendYtLinkAsync(username);
                }
            }
            else
            {
                var user = await this.FindUserAsync();
                if (user != null)
                {
                    await SendYtLinkAsync(user.LastFm);
                    return;
                }
                await this.Error(LastFmError.NotLinked);
            }
        }
    }
}