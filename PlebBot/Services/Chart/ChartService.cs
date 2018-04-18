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
            
            int imageHeight;
            int imageWidth;
            var template = images.FirstOrDefault(img => img.Img != null);
            if (template.Img != null)
            {
                imageHeight = template.Img.Height;
                imageWidth = template.Img.Width;
            }
            else
            {
                imageHeight = 300;
                imageWidth = 300;
            }               
            var widthExtension = 0;
            if (caption) widthExtension = 200; //how much room there is for the text
            var canvasHeight = imageHeight * chartSize;
            var canvasWidth = imageWidth * chartSize + widthExtension;
            
            var tempSurface = SKSurface.Create(new SKImageInfo(canvasWidth + widthExtension, canvasHeight));
            var canvas = tempSurface.Canvas;
            canvas.Clear(SKColors.Black);

            if (caption) await DrawCaption(canvas, images, chartSize, imageHeight);
            
            var offset = 0;
            var offsetTop = 0;
            var i = 1;
            foreach (var item in images)
            {
                var rect = SKRect.Create(offset, offsetTop, imageWidth, imageHeight);
                
                if (item.Img != null) canvas.DrawBitmap(item.Img, rect);
                else
                {
                    var placeholder = SKBitmap.Decode("Utilities/unavailable.jpg");
                    var paint = new SKPaint
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.High
                    };
                    canvas.DrawBitmap(placeholder, rect, paint);
                    placeholder.Dispose();
                }

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

        private static Task DrawCaption(SKCanvas canvas, List<(string Cap, SKBitmap Img)> images, int chartSize, int imageHeight)
        {
            var textXOffset = images.First().Img.Width * chartSize + 20f;
            var textYOffset = imageHeight * 0.5f - 25f;
            var font = new SKPaint
            {
                TextSize = 12.0f,
                IsAntialias = true,
                Color = SKColors.White,
                IsStroke = false,
                Typeface = SKTypeface.FromFile("Utilities/Roboto-Regular.ttf"),
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
                    textYOffset -= chartSize * font.TextSize * 1.4f;
                    textYOffset += imageHeight;
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