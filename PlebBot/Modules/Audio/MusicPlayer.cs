using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    public class MusicPlayer : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _audioService;
        private readonly YtService _ytService;

        //allows different servers to have different playing queues
        private readonly ConcurrentDictionary<ulong, ConcurrentQueue<string>> _queues = new ConcurrentDictionary<ulong, ConcurrentQueue<string>>();

        public MusicPlayer(AudioService audioService, YtService ytService)
        {
            this._audioService = audioService;
            this._ytService = ytService;
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("join")]
        [Summary("Get the bot in the voice chat")]
        public async Task Join()
        {
            await JoinAsync(Context);
            await Response.Success(
                Context, $"Joined your voice channel, {Context.User.Mention}. Now let's get the party going!");
        }

        //TODO: parallel video download, audio conversion and download to improve the speed of the bot
        [Command("play", RunMode = RunMode.Async)]
        [Alias("queue", "q")]
        [Summary("Play a song")]
        public async Task PlayMusic([Remainder] [Summary("Name or link of the video you want to play")] string name)
        {
            if (!await _audioService.CheckIfInVoice(Context.Guild)) await JoinAsync(Context);

            try
            {
                var path = await _ytService.DownloadVideoAsync(name);
                var audio = $"{path}.mkv";

                //await EnqueueAsync(name);

                Parallel.Invoke(
                    async () =>
                    {
                        await SendNowPlaying(name);
                    },
                    async () =>
                    {
                        await _audioService.ConvertToAudioAsync(path);
                    });

                await _audioService.StreamAudioAsync(Context.Guild, audio);


                //await DequeueAsync();
            }
            catch (Exception ex)
            {
                await Response.Error(Context, "The requested video could not be played.");
            }
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Alias("leave")]
        [Summary("Stop playback")]
        public async Task LeaveVoice()
        {
            await _audioService.LeaveVoiceAsync(Context.Guild);
        }

        private Task EnqueueAsync(string name)
        {
            if (_queues.TryGetValue(Context.Guild.Id, out var queue))
                _queues.AddOrUpdate(Context.Guild.Id, queue, (k, v) =>
                {
                    v.Enqueue(name);
                    return v;
                });
            return Task.CompletedTask;
        }

        private Task DequeueAsync()
        {
            if (_queues.TryGetValue(Context.Guild.Id, out var queue))
                _queues.AddOrUpdate(Context.Guild.Id, queue, (k, v) =>
                {
                    v.TryDequeue(out var temp);
                    return v;
                });
            return Task.CompletedTask;
        }

        private async Task SendNowPlaying(string name)
        {
            var video = await _ytService.GetVideoAsync("", name);
            var uploadDate = video.Snippet.PublishedAt.Value.ToString("MMMM dd, yyyy");
            var response = new EmbedBuilder();

            response
                .WithUrl($"https://youtu.be/{video.Id}")
                .WithTitle("Now playing")
                .WithThumbnailUrl(video.Snippet.Thumbnails.Medium.Url)
                .AddField("Title:", video.Snippet.Title)
                .AddInlineField("Views", video.Statistics.ViewCount)
                .AddInlineField("Likes", video.Statistics.LikeCount)
                .AddInlineField("Dislikes", video.Statistics.DislikeCount)
                .WithFooter($"Uploaded by {video.Snippet.ChannelTitle} on {uploadDate}")
                .WithColor(Color.Red);

            await ReplyAsync("", false, response.Build());
        }

        private async Task JoinAsync(SocketCommandContext context)
        {
            await _audioService.JoinVoiceAsync(context.Guild, (context.User as IVoiceState)?.VoiceChannel);
            _queues.TryAdd(Context.Guild.Id, new ConcurrentQueue<string>());
        }
    }
}