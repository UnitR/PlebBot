using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using PlebBot.Helpers;

namespace PlebBot.Modules
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        public async Task JoinVoiceAsync(IGuild guild, IVoiceChannel channel)
        {
            IAudioClient client;
            if (_connectedChannels.TryGetValue(guild.Id, out client)) return;
            if (channel.Guild.Id != guild.Id) return;
            var audioClient = await channel.ConnectAsync();
            _connectedChannels.TryAdd(guild.Id, audioClient);
        }

        public async Task LeaveVoiceAsync(IGuild guild)
        {
            IAudioClient client;
            if (_connectedChannels.TryRemove(guild.Id, out client))
                await client.StopAsync();
        }

        public async Task StreamAudioAsync(IGuild guild, string path)
        {
            IAudioClient client;
            if (_connectedChannels.TryGetValue(guild.Id, out client))
            {
                using (var output = CreateStream(path).StandardOutput.BaseStream)
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try { await output.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }
                }
            }
        }

        public Task<string> ConvertToAudioAsync(string path)
        {
            Process process;
            var audio = $"{path}.mp3";
            var args = $"-i \"{path}\" -q:a 0 -vn -ab 320k -ar 48000 -y \"{audio}\"";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                process = Process.Start(new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = $"ffmpeg {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
            }
            else
            {
                process = Process.Start(new ProcessStartInfo()
                {
                    FileName = "ffmpeg.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
            }
            process?.WaitForExit();
            return Task.FromResult(audio);
        }

        public Task<bool> CheckIfInVoice(IGuild guild)
        {
            return Task.FromResult(_connectedChannels.TryGetValue(guild.Id, out IAudioClient client));
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
    }
}