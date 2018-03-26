using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using System.IO;
using System.Linq;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;
using PlebBot.Services.Chart;
using PlebBot.TypeReaders;

namespace PlebBot.Modules
{
    [Group("chart")]
    public class Chart : BaseModule
    {
        private readonly HttpClient httpClient;
        private readonly Repository<User> userRepo;
        private readonly ChartService chartService;

        public Chart(HttpClient client, Repository<User> repo, ChartService service)
        {
            httpClient = client;
            userRepo = repo;
            chartService = service;
        }

        [Command("set", RunMode = RunMode.Async)]
        [Summary("Link a chart to your account.")]
        public async Task SetChart(string chartLink = "")
        {
            if (!await Preconditions.Preconditions.InCharposting(Context)) return;

            if (chartLink == String.Empty)
            {
                var temp = Context.Message.Attachments.First().Filename;
                if (temp.EndsWith("png") || temp.EndsWith(".jpg") || temp.EndsWith(".jpeg") || temp.EndsWith(".bmp"))
                    chartLink = Context.Message.Attachments.First().Url;
            }

            var imageBytes = await httpClient.GetByteArrayAsync(chartLink);

            if (imageBytes.Length == 0)
            {
                await Error("No chart image provided");
                return;
            }

            var user = await FindUserAsync();
            if (user == null)
                await userRepo.Add(new[] {"DiscordId", "Chart"}, new object[] {(long) Context.User.Id, imageBytes});
            else
                await userRepo.UpdateFirst("Chart", imageBytes, "DiscordId", (long) Context.User.Id);

            await Success("Successfully saved the chart.");
        }

        [Command(RunMode = RunMode.Async)]
        [Summary("Send your chart")]
        public async Task SendChart()
        {
            if (!await Preconditions.Preconditions.InCharposting(Context)) return;

            var user = await userRepo.FindFirst("DiscordId", Context.User.Id);
            if (user?.Chart == null)
            {
                await Error("You haven't saved a chart to your profile.");
                return;
            }

            using (Stream stream = new MemoryStream(user.Chart))
            {
                await Context.Channel.SendFileAsync(stream, $"{Context.User.Username}_chart.png",
                    $"{Context.User.Mention}'s chart:");

            }
        }

        [Group("top")]
        public class TopCharts : Chart
        {
            public TopCharts(HttpClient client, Repository<User> repo, ChartService service)
                : base(client, repo, service)
            {
            }

            [Command(RunMode = RunMode.Async)]
            public async Task Top(ChartType type, string span, 
                [OverrideTypeReader(typeof(ChartSizeReader))] ChartSize size)
            {
                if (!await Preconditions.Preconditions.InCharposting(Context)) return;

                var user = await FindUserAsync();
                if (user.LastFm == null) await Error("You'll need to link your last.fm profile first.");

                var result = await chartService.GetChartAsync(size, type, user.LastFm, span);
                using (Stream stream = new MemoryStream(result))
                {
                    await Context.Channel.SendFileAsync(
                        stream, 
                        $"{user}_top_{type}_{size}.png", 
                        $"Top {type.ToString().ToLowerInvariant()} for {Context.User.Username}:");
                }
            }
        }
    }
}