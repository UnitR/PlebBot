using System;
using System.Data.Common;
using Npgsql;

namespace DapperTest
{
    public class BotContext
    {
        public static Func<DbConnection> ConnectionFactory = () => new NpgsqlConnection(ConnectionString.Connection);
    }
}
