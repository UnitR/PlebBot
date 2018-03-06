using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PlebBot.Data.Models;

namespace PlebBot.Data.Repositories
{
    public class Repository<T> : IRepository<T>
    {
        private readonly string table;
        private Dictionary<string, object> valuesDict;

        //Determines which database table to use according to the passed type
        public Repository()
        {
            var type = typeof(T);
            if (type == typeof(User)) this.table = "Users";
            if (type == typeof(Role)) this.table = "Roles";
            if (type == typeof(Server)) this.table = "Servers";
        }

        public Task<int> Add(string column, object value)
            => Add(new[] {column}, new[] {value});

        public async Task<int> Add(string[] columns, object[] values)
        {
            var sql = $"insert into public.\"{this.table}\" (";
            foreach (var column in columns)
            {
                sql += $"\"{column}\",";
            }
            sql = sql.Remove(sql.Length - 1);
            sql += ") values (";
            valuesDict = new Dictionary<string, object>();
            for (int i = 0; i < values.Length; i++)
            {
                sql += $"@val{i},";
                valuesDict.Add($"val{i}", values[i]);
            }
            sql = sql.Remove(sql.Length - 1);
            sql += ")";

            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql, valuesDict);
            }

            this.valuesDict = null;
            return result;
        }

        public async Task<T> FindFirst(string condition)
        {
            T entity;
            var sql = $"select * from \"{this.table}\" where {condition}";

            using (var conn = BotContext.OpenConnection())
            {
                entity = await conn.QuerySingleOrDefaultAsync<T>(sql);
            }

            return entity;
        }

        public async Task<IEnumerable<T>> FindAll(string condition)
        {
            IEnumerable<T> entities;
            var sql = $"select * from \"{table}\" where {condition}";
            using (var conn = BotContext.OpenConnection())
            {
                var result = await conn.QueryMultipleAsync(sql);
                entities = await result.ReadAsync<T>();
            }
            return entities;
        }

        public async Task<int> DeleteFirst(string condition)
        {
            var sql = $"select * from \"{this.table}\" where {condition}";
            var result = 0;
            using (var conn = BotContext.OpenConnection())
            {
                var entity = await conn.QueryFirstOrDefaultAsync(sql);
                if(entity != null)
                {
                    result = 
                        await conn.ExecuteAsync($"delete from \"{this.table}\" where \"Id\" = @id",
                        new {id = entity.Id});
                }
            }

            return result;
        }

        public async Task<int> DeleteAll(string condition)
        {
            var sql = $"delete from \"{this.table}\" where {condition}";

            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql);
            }

            return result;
        }

        public Task<int> UpdateFirst(string column, object value, string condition)
            => UpdateFirst(new[] {column}, new[] {value}, condition);

        public async Task<int> UpdateFirst(string[] columns, object[] values, string condition)
        {
            var sql = await BuildUpdateSql(columns, values);
            var result = 0;
            using (var conn = BotContext.OpenConnection())
            {
                var entity = await conn.QuerySingleOrDefaultAsync($"select \"Id\" from \"{this.table}\" where {condition}");
                if (entity != null)
                {
                    sql += $" where \"Id\" = {entity.Id}";
                    result = await conn.ExecuteAsync(sql, valuesDict);
                }
            }

            this.valuesDict = null;
            return result;
        }

        public Task<int> UpdateAll(string column, object value, string condition)
            => UpdateAll(new[] {column}, new[] {value}, condition);

        public async Task<int> UpdateAll(string[] columns, object[] values, string condition)
        {
            var sql = await BuildUpdateSql(columns, values);
            sql += $" where {condition}";

            int result;
            using (var conn = BotContext.OpenConnection())
            {
                result = await conn.ExecuteAsync(sql, valuesDict);
            }

            this.valuesDict = null;
            return result;
        }

        private Task<string> BuildUpdateSql(string[] columns, object[] values)
        {
            var sql = $"update \"{this.table}\" set ";
            valuesDict = new Dictionary<string, object>();
            for (int i = 0; i < columns.Length; i++)
            {
                sql += $"\"{columns[i]}\" = @val{i},";
                valuesDict.Add($"val{i}", values[i]);
            }
            sql = sql.Remove(sql.Length - 1);

            return Task.FromResult(sql);
        }
    }
}