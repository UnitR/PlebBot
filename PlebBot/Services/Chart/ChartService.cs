using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SkiaSharp;

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

        public async Task<byte[]> GetChartAsync(ChartSize size, ChartType type, string username, string span, boolean caption)
            => await GetChart(size, type, username, span, caption);

        private async Task<byte[]> GetChart(ChartSize size, ChartType type, string username, string span, boolean caption)
        {
            var imageDictionary = await GetAlbumArt(size, type, username, span, caption);
            if (imageDictionary == null) return null;
            var result = await BuildChart(imageDictionary, (int) size);
            return result;
        }

        //TODO: add names to charts
        private static Task<byte[]> BuildChart(Dictionary<string, byte[]> imageDictionary, int chartSize)
        {
            var images = new List<(string Cap, SKBitmap Img)>(imageDictionary.Count);

            images.AddRange(
                from image in imageDictionary where (image.Key.Length != 0 && image.Value.Length != 0) select (image.Key, SKBitmap.Decode(image.Value)));
            
            var height = images[0].Img.Height * chartSize;
            var width = images[0].Img.Width * chartSize;

            var tempSurface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = tempSurface.Canvas;
            canvas.Clear(SKColors.Black);
            var offset = 0;
            var offsetTop = 0;
            var i = 1;

            var font = new SKPaint();
            font.TextSize = 64.0f;
            font.IsAntialias = true;
            font.Color = new SKColor(0xFFFFFFFF);
            font.IsStroke = false;

            foreach (var pair in images)
            {
                if (pair.img == null) continue;
                canvas.DrawBitmap(pair.img, SKRect.Create(offset, offsetTop, pair.img.Width, pair.img.Height));
                canvas.DrawText(pair.cap, offset, offsetTop, font);
                canvas.Flush();
                offset += images[i].Img.Width;
                if (i == chartSize)
                {
                    offset = 0;
                    offsetTop += pair.img.Height;
                    i = 1;
                    continue;
                }
                i++;
            }
            var result = tempSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 70).ToArray();

            tempSurface.Dispose();
            foreach (var image in images)
            {
                image?.Dispose();
            }

            return Task.FromResult(result);
        }

        private async Task<Dictionary<string, byte[]>> GetAlbumArt(
            ChartSize size, ChartType type, string username, string span, boolean caption)
        {
            var intSize = (int) size;
            intSize *= intSize;
            var response = await lastFm.GetTopAsync(type, intSize, span, username);
            var imageDictionary = new Dictionary<string, byte[]>(intSize);

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
                string url = null;
                byte[] art = null;
                string txt = null;

                for (var j = 2; j >= 0; j--)
                {
                    url = response[i].image[j]["#text"].ToString();
                    if (!String.IsNullOrEmpty(url)) break;
                }

                if (!String.IsNullOrEmpty(url))
                {
                    art = await httpClient.GetByteArrayAsync(url);
                }

                var name = new StringBuilder();
                name.Append($"{response[i].name}");
                if (response[i].artist != null) name.Append($"\n{response[i].artist.name}");
                if (art == null) art = new byte[] { };
                imageDictionary.Add(name.ToString(), art);
            }

            return imageDictionary;
        }
    }
}