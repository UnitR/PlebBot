using System.Linq;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlebBot.Data;
using PlebBot.Data.Models;

namespace PlebBot.Tests
{
    [TestClass]
    public class DapperTests
    {
        [TestMethod]
        public void GetEntity()
        {
            User user;
            using (var conn = BotContext.OpenConnection())
            {
                var query =
                    conn.Query<User>("select \"DiscordId\", \"LastFm\" " +
                                     "from public.\"Users\" where \"DiscordId\" = @discordId",
                                     new { discordId = "164102776035475458" });
                user = query.First();
            }

            var expected = new User()
            {
                DiscordId = "164102776035475458",
                LastFm = "UnitR_",
            };
            var actual = user;

            Assert.AreEqual(expected.DiscordId, actual.DiscordId, "Incorrect DiscordId");
            Assert.AreEqual(expected.LastFm, actual.LastFm, "Incorrect LastFm");
        }
    }
}
