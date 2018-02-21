using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Test.Models;

namespace Dapper.Test
{
    public class EntityGetTest
    {
        public Task GetEntity()
        {
            User user;
            using (var db = BotContext.ConnectionFactory())
            {
                var dbQuery = db.Query<User>("select DiscordId = @DiscordId", new {DiscordId = "164102776035475458"});
                user = dbQuery.First();
            }
            Console.WriteLine(user.LastFm);
            return Task.CompletedTask;
        }
    }
}