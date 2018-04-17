using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlebBot.Data.Repository
{
    public interface IRepository<T>
    {
        //Add a new record to the database
        Task<int> Add(IEnumerable<string> columns, IEnumerable<object> values);

        //Find the first matching record from the database
        Task<T> FindFirst(string column, object value);

        //Fid all matching records from the database
        Task<IEnumerable<T>> FindAll(string column, object value);

        //Delete the first matching record from the database
        Task<int> DeleteFirst(string column, object value);

        //Delete all matching records from the database
        Task<int> DeleteAll(string column, object value);

        //Update the first record in the database
        Task<int> UpdateFirst(
            IEnumerable<string> columns, IEnumerable<object> values, string findColumn, object findValue);

        //Update all matching records from the database
        Task<int> UpdateAll(
            IEnumerable<string> columns, IEnumerable<object> values, string findColumn, object findValue);
    }
}