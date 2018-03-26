using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PlebBot.Data.Models;

namespace PlebBot.Data.Repository
{
    public class Repository<T> : IRepository<T>
    {
        private readonly string table;
        private Dictionary<string, object> valuesDict;

        //Determines which database table to use according to the passed type
        public Repository()
        {
            var type = typeof(T);
            if (type == typeof(User)) table = "Users";
            if (type == typeof(Role)) table = "Roles";
            if (type == typeof(Server)) table = "Servers";
        }

        public Task<int> Add(string column, object value)
            => Add(new[] {column}, new[] {value});

        public async Task<int> Add(string[] columns, object[] values)
        {
            var sql = new StringBuilder($"insert into public.\"{table}\" (");
            foreach (var column in columns)
            {
                sql.Append($"\"{column}\",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(") values (");
            valuesDict = new Dictionary<string, object>();
            for (var i = 0; i < values.Length; i++)
            {
                sql.Append($"@val{i},");
                valuesDict.Add($"val{i}", values[i]);
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");

            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql.ToString(), valuesDict);
            }

            valuesDict = null;
            return result;
        }

        public async Task<T> FindByDiscordId(long discordId)
            => await FindFirst("DiscordId", discordId);

        public async Task<T> FindFirst(string column, object value)
        {
            var sql = await BuildFindSql(column, value);
            T entity;
            using (var conn = BotContext.OpenConnection())
            {
                entity = await conn.QuerySingleOrDefaultAsync<T>(sql, new {value});
            }
            return entity;
        }

        public async Task<IEnumerable<T>> FindAll(string column, object value)
        {
            var sql = await BuildFindSql(column, value);
            IEnumerable<T> entities;
            using (var conn = BotContext.OpenConnection())
            {
                var result = await conn.QueryMultipleAsync(sql, new {value});
                entities = await result.ReadAsync<T>();
            }
            return entities;
        }

        private Task<string> BuildFindSql(string column, object value)
        {
            string sql;
            if (value is string || value is char)
                sql = $"select * from \"{table}\" where lower(\"{column}\") = @value";
            else
                sql = $"select * from \"{table}\" where \"{column}\" = @value";

            return Task.FromResult(sql);
        }

        public async Task<int> DeleteFirst(string column, object value)
        {
            var sql = await BuildDeleteSql(column);
            var result = 0;
            using (var conn = BotContext.OpenConnection())
            {
                var entity = await conn.QueryFirstOrDefaultAsync(sql, new {value});
                if(entity != null)
                {
                    result = 
                        await conn.ExecuteAsync($"delete from \"{table}\" where \"Id\" = @id",
                        new {id = entity.Id});
                }
            }
            return result;
        }

        public async Task<int> DeleteAll(string column, object value)
        {
            var sql = await BuildFindSql(column, value);
            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql, new {value});
            }

            return result;
        }

        private Task<string> BuildDeleteSql(string column)
        {
            var sql = $"delete from \"{table}\" where \"{column}\" = @value";
            return Task.FromResult(sql);
        }

        public Task<int> UpdateFirst(string column, object value, string findColumn, object findValue)
            => UpdateFirst(new[] {column}, new[] {value}, findColumn, findValue);

        public async Task<int> UpdateFirst(string[] columns, object[] values, string findColumn, object findValue)
        {
            var sql = new StringBuilder(await BuildUpdateSql(columns, values));
            var result = 0;
            using (var conn = BotContext.OpenConnection())
            {
                dynamic entity = await FindFirst(findColumn, findValue);
                if (entity != null)
                {
                    sql.Append($" where \"{findColumn}\" = @id");
                    valuesDict.Add("id", entity.Id);
                    result = await conn.ExecuteAsync(sql.ToString(), valuesDict);
                }
            }

            valuesDict = null;
            return result;
        }

        public Task<int> UpdateAll(string column, object value, string findColumn, object findValue)
            => UpdateAll(new[] {column}, new[] {value}, findColumn, findValue);

        public async Task<int> UpdateAll(string[] columns, object[] values, string findColumn, object findValue)
        {
            var sql = new StringBuilder(await BuildUpdateSql(columns, values));
            sql.Append($" where \"{findColumn}\" = @findValue");
            valuesDict.Add("findValue", findValue);

            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql.ToString(), valuesDict);
            }

            valuesDict = null;
            return result;
        }

        private Task<string> BuildUpdateSql(IReadOnlyList<string> columns, IReadOnlyList<object> values)
        {
            var sql = new StringBuilder($"update \"{table}\" set ");
            valuesDict = new Dictionary<string, object>();
            for (var i = 0; i < columns.Count; i++)
            {
                sql.Append($"\"{columns[i]}\" = @val{i},");
                valuesDict.Add($"val{i}", values[i]);
            }
            sql.Remove(sql.Length - 1, 1);

            return Task.FromResult(sql.ToString());
        }
    }
}