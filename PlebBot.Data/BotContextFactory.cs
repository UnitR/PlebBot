using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PlebBot.Data
{
    class BotContextFactory : IDesignTimeDbContextFactory<BotContext>
    {
        public BotContext CreateDbContext(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("_config.json")
                .Build();
            var builder = new DbContextOptionsBuilder<BotContext>();
            var connString = config["connection_string"];
            builder.UseNpgsql(connString);

            return new BotContext(builder.Options);
        }
    }
}
