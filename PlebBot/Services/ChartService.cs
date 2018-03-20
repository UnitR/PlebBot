using System.Drawing;
using PlebBot.Services.LastFm;

namespace PlebBot.Services
{
    public class ChartService
    {
        private readonly LastFmService lastFm;

        public ChartService(LastFmService service)
        {
            lastFm = service;
        }

        //public byte[] WeekTop(ChartSize size, ChartType type, string username)
        //{
        //    var top = await lastFm.GetTopAsync(type, (int) size, TimeSpan.Week, username)
        //}
    }

    public enum ChartSize
    {
        Small = 3*3,
        Medium = 4*4,
        Big = 5*5
    }
}