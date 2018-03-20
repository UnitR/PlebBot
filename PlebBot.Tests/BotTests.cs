using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlebBot.Data.Models;
using PlebBot.Data.Repositories;

namespace PlebBot.Tests
{
    [TestClass]
    public class BotTests
    {
        [TestMethod]
        public void SaveChart()
        {
            var httpClient = new HttpClient();
            var userRepo = new Repository<User>();
            const string chartLink = 
                @"https://cdn.discordapp.com/attachments/417956085253668864/425440364548194304/UnitR_chart.png";
            var bytes = httpClient.GetByteArrayAsync(chartLink).Result;
            var condition = $"\"DiscordId\" = 164102776035475458";
            userRepo.UpdateFirst("Chart", bytes, condition);

            var user = userRepo.FindFirst(condition).Result;
            Assert.AreEqual(bytes, user.Chart, "Byte arrays have different values.");
        }
    }
}