using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net.Mime;
using Discord;
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

        public async Task<byte[]> GetChartAsync(ChartSize size, ChartType type, string username, string span, bool caption)
            => await GetChart(size, type, username, span, caption);

        private async Task<byte[]> GetChart(ChartSize size, ChartType type, string username, string span, bool caption)
        {
            var imageDictionary = await GetAlbumArt(size, type, username, span, caption);
            if (imageDictionary == null) return null;
            var result = await BuildChart(imageDictionary, (int) size, caption);
            return result;
        }

        private async Task<byte[]> BuildChart(List<(string Caption, byte[] Image)> imageList, int chartSize, bool caption)
        {
            var images = new List<(string Cap, SKBitmap Img)>(imageList.Count);
            images.AddRange(from image in imageList select (image.Caption, SKBitmap.Decode(image.Image)));
            
            var height = images[0].Img.Height * chartSize;
            var width = images[0].Img.Width * chartSize;
            var widthExtension = 0;
            if (caption) widthExtension = 400; //how much room there is for the text

            var tempSurface = SKSurface.Create(new SKImageInfo(width + widthExtension, height));
            var canvas = tempSurface.Canvas;
            canvas.Clear(SKColors.Black);

            if (caption) await DrawCaption(canvas, images, chartSize);
            
            var imageHeight = images[0].Img.Width;
            var imageWidth = images[0].Img.Height;
            var offset = 0;
            var offsetTop = 0;
            var i = 1;
            foreach (var item in images)
            {
                if (item.Img != null)
                    canvas.DrawBitmap(item.Img, SKRect.Create(offset, offsetTop, imageWidth, imageHeight));
                else 
                    canvas.DrawRect(offset, offsetTop, imageWidth, imageHeight, new SKPaint {Color = SKColors.Black});

                offset += imageWidth;
                if (i == chartSize)
                {
                    offset = 0;
                    offsetTop += imageHeight;
                    i = 1;
                    continue;
                }
                i++;
            }
            canvas.Flush();
 
            var result = tempSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 70).ToArray();

            tempSurface.Dispose();
            foreach (var item in images)
            {
                item.Img?.Dispose();
            }

            return result;
        }

        private static Task DrawCaption(SKCanvas canvas, List<(string Cap, SKBitmap Img)> images, int chartSize)
        {
            var textXOffset = images.First().Img.Width * chartSize + 20f;
            var textYOffset = 40f;
            var font = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.White,
                IsStroke = false,
                Typeface = SKTypeface.FromFile("./Utilities/Roboto-Regular.ttf"),
                LcdRenderText = true,
                SubpixelText = true
            };

            var i = 1;
            foreach (var item in images)
            {
                canvas.DrawText(item.Cap, textXOffset, textYOffset, font);
                textYOffset += font.TextSize * 1.4f;
                
                if (i == chartSize)
                {
                    textYOffset += chartSize * 10f + 10;
                    i = 1;
                    continue;
                }
                i++;
            }

            return Task.CompletedTask;
        }

        private async Task<List<(string Caption, byte[] Image)>> GetAlbumArt(
            ChartSize size, ChartType type, string username, string span, bool caption)
        {
            var intSize = (int) size;
            intSize *= intSize;
            var response = await lastFm.GetTopAsync((ListType) type, intSize, span, username);
            var imageList = new List<(string Caption, byte[] Image)>(intSize);

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
                    if (!String.IsNullOrEmpty(url)) break;
                }

                if (!String.IsNullOrEmpty(url))
                {
                    art = await httpClient.GetByteArrayAsync(url);
                }

                var name = new StringBuilder();
                if (response[i].artist != null) name.Append($"{response[i].artist.name} - ");
                name.Append($"{response[i].name}");
                if (art == null) art = new byte[] { };
                imageList.Add((name.ToString(), art));
            }

            return imageList;
        }
    }
}