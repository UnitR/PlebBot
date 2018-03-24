using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<byte[]> GetChartAsync(ChartSize size, ChartType type, string username, string span)
            => await GetChart(size, type, username, span);

        private async Task<byte[]> GetChart(ChartSize size, ChartType type, string username, string span)
        {
            var imageDictionary = await GetAlbumArt(size, type, username, span);
            if (imageDictionary == null) return null;
            var result = await BuildChart(imageDictionary, (int) size);
            return result;
        }

        //TODO: add names to charts
        private Task<byte[]> BuildChart(Dictionary<string, byte[]> imageDictionary, int chartSize)
        {
            var images = new List<SKBitmap>(chartSize * chartSize);
            byte[] result;

            images.AddRange(
                from image in imageDictionary where image.Value != null select SKBitmap.Decode(image.Value));

            var height = images[0].Height * chartSize;
            var width = images[0].Width * chartSize;

            using (var tempSurface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = tempSurface.Canvas;
                canvas.Clear(SKColors.Black);
                var offset = 0;
                var offsetTop = 0;
                var i = 1;
                foreach (var image in images)
                {
                    canvas.DrawBitmap(image, SKRect.Create(offset, offsetTop, image.Width, image.Height));
                    offset += images[i].Width;
                    if (i == chartSize)
                    {
                        offset = 0;
                        offsetTop += image.Height;
                        i = 1;
                        continue;
                    }
                    i++;
                }
                result = tempSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 70).ToArray();
            }

            foreach (var image in images)
            {
                image.Dispose();
            }

            return Task.FromResult(result);
        }

        private async Task<Dictionary<string, byte[]>> GetAlbumArt(
            ChartSize size, ChartType type, string username, string span)
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

                for (var j = 2; j >= 0; j--)
                {
                    url = response[i].image[j]["#text"].ToString();
                    if (!string.IsNullOrEmpty(url)) break;
                }

                if (!String.IsNullOrEmpty(url))
                {
                    art = await httpClient.GetByteArrayAsync(url);
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