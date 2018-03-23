using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

namespace PlebBot.Services.Chart
{
    public class ChartService
    {
        private readonly LastFmService lastFm;
        private readonly HttpClient httpClient;

        public ChartService(LastFmService service, HttpClient client)
        {
            lastFm = service;
            httpClient = client;
        }

        public async Task<byte[]> GetChartAsync(ChartSize size, ChartType type, string username, string span)
            => await GetChart(size, type, username, span);

        private async Task<byte[]> GetChart(ChartSize size, ChartType type, string username, string span)
        {
            var imageDictionary = await GetAlbumArt(size, type, username, span);
            if (imageDictionary == null) return null;
            var result = await BuildChart(imageDictionary, (int) size);
            return result;
        }

        private async Task<byte[]> BuildChart(Dictionary<string, byte[]> imageDictionary, int chartSize)
        {
            byte[] bytes;
            using (var images = new MagickImageCollection())
            {
                foreach (var image in imageDictionary)
                {
                    MagickImage img;
                    if (image.Value == null)
                    {
                        img = new MagickImage(MagickColors.Black, 300, 300);
                    }
                    else img = await GenerateImage(image.Value);
                    new Drawables()
                        .TextAlignment(TextAlignment.Left)
                        .StrokeColor(MagickColors.White)
                        .FillColor(MagickColors.White)
                        .TextAntialias(true)
                        .Font("Tahoma")
                        .FontPointSize(12.5)
                        .Text(0, 11, image.Key)
                        .TextKerning(1.5)
                        .TextUnderColor(MagickColors.Black)
                        .StrokeAntialias(true)
                        .Draw(img);
                    images.Add(img);
                }

                var settings = new MontageSettings()
                {
                    TileGeometry = new MagickGeometry(chartSize, chartSize),
                    BackgroundColor = MagickColors.Black,
                    Geometry = new MagickGeometry(900, 900)
                };
                using (var result = images.Montage(settings))
                {
                    result.Scale(new Percentage(20));
                    bytes = result.ToByteArray(MagickFormat.Png);
                }
            }
            return bytes;
        }

        private Task<MagickImage> GenerateImage(byte[] image)
        {
            var readSettings = new MagickReadSettings()
            {
                Width = 300,
                Height = 300,
            };

            var fileType = image.GetFileType();
            switch (fileType.Extension)
            {
                case "png":
                    readSettings.Format = MagickFormat.Png;
                    break;
                case "jpeg":
                    readSettings.Format = MagickFormat.Jpeg;
                    break;
                case "gif":
                    readSettings.Format = MagickFormat.Gif;
                    break;
                case "bmp":
                    readSettings.Format = MagickFormat.Bmp;
                    break;
            }

            var img = new MagickImage(image, readSettings);
            if(img.Format != MagickFormat.Png) img.Format = MagickFormat.Png;
            return Task.FromResult(img);
        }

        private async Task<Dictionary<string, byte[]>> GetAlbumArt(
            ChartSize size, ChartType type, string username, string span)
        {
            var intSize = (int) size;
            intSize *= intSize;
            var response = await lastFm.GetTopAsync(type, intSize, span, username);
            var imageDictionary = new Dictionary<string, byte[]>();

            switch (type)
            {
                case ChartType.Albums:
                    response = response.topalbums.album;
                    break;
                case ChartType.Artists:
                    response = response.topartists.artist;
                    break;
                default:
                    return null;
            }

            for (var i = 0; i < response.Count; i++)
            {
                var url = response[i].image[2]["#text"] ?? null;
                byte[] art = null;
                if (url != null)
                {
                    art = await httpClient.GetByteArrayAsync(url.ToString());
                }
                var name = new StringBuilder();
                name.Append($" {response[i].name} ");
                if (response[i].artist != null) name.Append($"\n {response[i].artist.name} ");
                imageDictionary.Add(name.ToString(), art);
            }

            return imageDictionary;
        }
    }
}