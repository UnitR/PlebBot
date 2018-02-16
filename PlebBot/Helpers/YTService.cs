using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace PlebBot.Helpers
{
    public class YtService
    {
        private readonly YouTubeService _service;

        public YtService()
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            _service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = config["tokens:yt_key"],
                ApplicationName = this.GetType().ToString()
            });
        }

        public async Task LinkVideoAsync(SocketCommandContext context, string query)
        {
            var result = await GetSearchResultsAsync(query);
            if (result.Items.Count > 0)
            {
                var videoId = result.Items.FirstOrDefault().Id.VideoId;
                var video = await GetVideoAsync(videoId);
                var videoDate = video.Snippet.PublishedAt.Value.ToString("MMMM dd, yyyy");

                var response = $"{video.Snippet.Title} | " +
                               $"{video.Statistics.ViewCount} views";
                if (video.Statistics.LikeCount != null) response += $" | {video.Statistics.LikeCount} likes";
                if (video.Statistics.DislikeCount != null) response += $" | {video.Statistics.DislikeCount} dislikes";
                response += $"\nUploaded on {videoDate} by {video.Snippet.ChannelTitle}\n" +
                            $"https://youtu.be/{videoId}";

                await context.Channel.SendMessageAsync(response);
                return;
            }
            await Response.Error(context, "No matching videos found.");
        }

        public async Task<string> DownloadVideoAsync(string query)
        {
            var searchResult = await GetSearchResultsAsync(query);
            var video = await GetVideoAsync(searchResult.Items[0].Id.VideoId);

            var client = new YoutubeClient();
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(video.Id);
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
            var ext = streamInfo.Container.GetFileExtension();
            var path = $"Temp/{video.Id}.{ext}";
            await client.DownloadMediaStreamAsync(streamInfo, path);
            return path;
        }

        public async Task<Video> GetVideoAsync(string videoId, string videoName = "")
        {
            if (videoId == String.Empty)
            {
                var search = await GetSearchResultsAsync(videoName);
                var result = search.Items.FirstOrDefault();
                videoId = result?.Id.VideoId;
            }
            var videoRequest = _service.Videos.List("statistics, snippet");
            videoRequest.Id = videoId;
            var videoResult = await videoRequest.ExecuteAsync();
            var video = videoResult.Items.FirstOrDefault();

            return video;
        }

        private async Task<SearchListResponse> GetSearchResultsAsync(string query)
        {
            var request = _service.Search.List("snippet");
            request.Q = query;
            request.MaxResults = 1;
            request.Type = "video";
            
            var result = await request.ExecuteAsync();

            return result;
        }
    }
}