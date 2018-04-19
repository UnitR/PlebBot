using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using System.IO;
using System.Linq;
using PlebBot.Services.Chart;
using PlebBot.TypeReaders;

namespace PlebBot.Modules
{
    [Group("Chart")]
    public class Chart : BaseModule
    {
        private readonly HttpClient httpClient;
        private readonly ChartService chartService;

        public Chart(HttpClient client, ChartService service)
        {
            httpClient = client;
            chartService = service;
        }

        [Command("set", RunMode = RunMode.Async)]
        [Summary("Link a chart to your account.")]
        public async Task SetChart(
            [Summary("The link to your chart. Either use this parameter or send the chart image with the message.")] string chartLink = "")
        {
            if (!await Preconditions.Preconditions.InChartposting(Context)) return;

            if (chartLink == String.Empty)
            {
                var temp = Context.Message.Attachments.First().Filename;
                if (temp.EndsWith("png") || temp.EndsWith(".jpg") || temp.EndsWith(".jpeg") || temp.EndsWith(".bmp"))
                    chartLink = Context.Message.Attachments.First().Url;
            }

            if (!(Uri.IsWellFormedUriString(chartLink, UriKind.Absolute)) || String.IsNullOrEmpty(chartLink))
            {
                await Error("No proper chart or link provided. Try again.");
                return;
            }

            byte[] imageBytes;
            try
            {
                imageBytes = await httpClient.GetByteArrayAsync(chartLink);
                await SaveUserData("Chart", imageBytes);
                await Success("Chart saved.");
            }
            catch (InvalidOperationException)
            {
                await Error("Your chart could not be downloaded. Check the chart image or link.");
            }
        }

        [Command(RunMode = RunMode.Async)]
        [Summary("Send your chart")]
        public async Task SendChart()
        {
            if (!await Preconditions.Preconditions.InChartposting(Context)) return;

            var user = await FindUserAsync();
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

        [Group("Top")]
        public class TopCharts : Chart
        {
            public TopCharts(HttpClient client, ChartService service)
                : base(client, service)
            {
            }

            [Command(RunMode = RunMode.Async)]
            [Summary("Send a chart based on your last.fm scrobbles.")]
            public async Task Top([Summary("The type of the chart. Either albums or artists.")] ChartType type, 
                [Summary("The time span for the chart. Overall, year, 6month, 3month, month or week.")] string span, 
                [OverrideTypeReader(typeof(ChartSizeReader))] [Summary("Chart size. Supported sizes are 3x3, 4x4 and 5x5.")] ChartSize size, 
                [Summary("Pass this argument if you want to include names next to your chart. Accepted values are `-c` and `-t`. Currently there is no difference between them.")]
                string caption = "")
            {
                if (!await Preconditions.Preconditions.InChartposting(Context)) return;

                var user = await FindUserAsync();
                if (user.LastFm == null) await Error("You'll need to link your last.fm profile first.");

                caption = caption.ToLowerInvariant();
                var withCaption = caption == "captions" || caption == "-c" || caption == "-t" || caption == "titles";
                    
                var result = await chartService.GetChartAsync(size, type, user.LastFm, span, withCaption);

                if (result == null)
                {
                    await Error(
                        "Something went wrong obtaining the chart information. Check the given parameters and try again");
                    return;
                }
                
                using (Stream stream = new MemoryStream(result))
                {
                    await Context.Channel.SendFileAsync(
                        stream, 
                        $"{Context.User.Username}_top_{type}_{size}.png", 
                        $"Top {type.ToString().ToLowerInvariant()} for {Context.User.Username}:");
                }
            }
        }
    }
}
