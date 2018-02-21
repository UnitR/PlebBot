using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlebBot.Data.Models;

namespace PlebBot.Data
{
    public class BotContext
    {
        public static Func<DbConnection> OpenConnection = () => new NpgsqlConnection(Connection());

        private static string Connection()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("_config.json")
                .Build();
            var connString = config["connection_string"];

            return connString;
        }
    }
}