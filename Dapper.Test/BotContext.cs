using System;
using System.Data.Common;
using System.IO;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Dapper.Test
{
    public class BotContext
    {
        public static Func<DbConnection> ConnectionFactory = () => new NpgsqlConnection(Connection());

        private static string Connection()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("_config.json")
                .Build();
            var connString = config["connection_strin"];

            return connString;
        }
    }
}
