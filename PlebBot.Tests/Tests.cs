using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlebBot.Data;
using PlebBot.Data.Models;
using PlebBot.Data.Repository;
using PlebBot.Services;

namespace PlebBot.Tests
{
    [TestClass]
    public class Tests
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
                                     new { discordId = 164102776035475458 });
                user = query.First();
            }

            var expected = new User()
            {
                DiscordId = 164102776035475458,
                LastFm = "UnitR_",
            };
            var actual = user;

            Assert.AreEqual(expected.DiscordId, actual.DiscordId, "Incorrect DiscordId");
            Assert.AreEqual(expected.LastFm, actual.LastFm, "Incorrect LastFm");
        }

        [TestMethod]
        public void Insert()
        {
            const ulong discordId = 369907714819751938;
            const string lastFm = "UnitR_";
            const string rym = "UnitR";
            const string table = "Users";
            string id;
            string[] columns = {"DiscordId", "LastFm", "Rym"};
            dynamic[] values = {discordId, lastFm, rym};
            var insertSql = $"insert into public.\"{table}\" (";

            foreach (var column in columns)
            {
                insertSql += $"\"{column}\",";
            }
            insertSql = insertSql.Remove(insertSql.Length - 1);
            insertSql += ") values (";
            var valuesDict = new Dictionary<string, dynamic>();
            for (var i = 0; i < values.Length; i++)
            {
                insertSql += $"@val{i},";
                valuesDict.Add($"val{i}", values[i]);
            }
            insertSql = insertSql.Remove(insertSql.Length - 1);
            insertSql += ")";

            using (var conn = BotContext.OpenConnection())
            {
                conn.Execute(insertSql, valuesDict);

                id = conn.QueryFirst<string>(
                    $"select \"DiscordId\" from public.\"{table}\"" +
                    " where \"DiscordId\" = @id",
                    new {id = discordId});
            }

            Assert.AreEqual(369907714819751938, id, "Wrong ID.");
        }

        [TestMethod]
        public void RepositoryInsert()
        {
            string[] columns = {"DiscordId", "LastFm"};
            dynamic[] values = {369907714819751938, "UnitR_"};
            var userRepo = new Repository<User>();
            var result = userRepo.Add(columns, values).Result;

            Assert.AreEqual(1, result, "Incorrect number of rows affected.");
        }

        [TestMethod]
        public void RepositoryFindFirst()
        {
            var roleRepo = new Repository<Role>();
            var result = roleRepo.FindFirst("Name", "Newsletter".ToLower()).Result;

            Assert.AreEqual("Newsletter", result.Name, "Different role names.");
        }

        [TestMethod]
        public void RepositoryFindAll()
        {
            const int id = 1;
            var userRepo = new Repository<User>();
            var result = userRepo.FindAll("Id", 1).Result;

            Assert.AreEqual(result.First().Id, id, "IDs don't match.");
        }

        [TestMethod]
        public void RepositoryDeleteFirst()
        {
            var userRepo = new Repository<User>();
            var result = userRepo.DeleteFirst("Id", 1).Result;

            Assert.AreEqual(1, result, "Incorrect number of rows affected.");
        }

        [TestMethod]
        public void RepositoryDeleteAll()
        {
            var userRepo = new Repository<User>();
            var result = userRepo.DeleteAll("DiscordId", 369907714819751938).Result;

            Assert.AreEqual(result, 4, "Incorrect number of rows affected.");
        }

        [TestMethod]
        public void RepositoryUpdateFirst()
        {
            object[] values = {"last.fm", "rym"};
            string[] columns = {"LastFm", "Rym"};
            var userRepo = new Repository<User>();
            var result = userRepo.UpdateFirst(columns, values, "Id", 1).Result;

            Assert.AreEqual(result, 1, "Incorrect number of rows affected.");
        }

        [TestMethod]
        public void FindRole()
        {
            var roleRepo = new Repository<Role>();
            const string role = "newsletter";
            var roleResult = roleRepo.FindFirst("Name", role.ToLowerInvariant()).Result;

            Assert.AreEqual(role, roleResult.Name.ToLowerInvariant(), "Incorrect role fetched.");
        }

        [TestMethod]
        public void Total()
        {
            var service = new LastFmService(new HttpClient());
            const string expected = "Week [804 scrobbles total]";
            var actual = $"Week {service.TotalScrobblesAsync("week", "UnitR_").Result}";
            
            Assert.AreEqual(expected, actual, "Improperly formatted top.");
        }
    }
}
