using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace PlebBot.Services
{
    public class YtService
    {
        private readonly YouTubeService service;

        public YtService()
        {
            var config = new ConfigurationBuilder().AddJsonFile("_config.json").Build();
            service = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = config["tokens:yt_key"],
                ApplicationName = GetType().ToString()
            });
        }

        public async Task<string> GetVideoLinkAsync(string query)
        {
            var result = await GetSearchResultsAsync(query);
            if (result.Items.Count <= 0) return null;
            var videoId = result.Items.FirstOrDefault()?.Id.VideoId;

            var video = await GetVideoAsync(videoId);
            var videoMinutes = XmlConvert.ToTimeSpan(video.ContentDetails.Duration).ToString(@"mm");
            if (videoMinutes[0] == '0') videoMinutes = videoMinutes.Remove(0, 1);
            var videoSeconds = XmlConvert.ToTimeSpan(video.ContentDetails.Duration).ToString(@"ss");

            var response = $"{String.Format("{0:n0}", video.Statistics.ViewCount)} views | " +
                           $"Duration: {videoMinutes} minutes {videoSeconds} seconds";
            if (video.Statistics.LikeCount != null)
                response += $" | {String.Format("{0:n0}", video.Statistics.LikeCount)} likes";
            if (video.Statistics.DislikeCount != null)
                response += $" | {String.Format("{0:n0}", video.Statistics.DislikeCount)} dislikes";

            response += $"\nhttps://youtu.be/{videoId}";
            return response;
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

        private async Task<Video> GetVideoAsync(string videoId, string videoName = "")
        {
            if (videoId == String.Empty)
            {
                var search = await GetSearchResultsAsync(videoName);
                var result = search.Items.FirstOrDefault();
                videoId = result?.Id.VideoId;
            }
            var videoRequest = service.Videos.List("statistics, snippet, contentDetails");
            videoRequest.Id = videoId;
            var videoResult = await videoRequest.ExecuteAsync();
            var video = videoResult.Items.FirstOrDefault();

            return video;
        }

        private async Task<SearchListResponse> GetSearchResultsAsync(string query)
        {
            var request = service.Search.List("snippet");
            request.Q = query;
            request.MaxResults = 1;
            request.Type = "video";

            var result = await request.ExecuteAsync();
            return result;
        }
    }
}