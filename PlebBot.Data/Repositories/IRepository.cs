using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlebBot.Data.Repositories
{
    public interface IRepository<T>
    {
        //Add a new record to the database
        Task<int> Add(string[] columns, dynamic[] values);

        //Find the first matching record from the database
        Task<T> FindFirst(string condition);

        //Fid all matching records from the database
        Task<IEnumerable<T>> FindAll(string condition);

        //Delete the first matching record from the database
        Task<int> DeleteFirst(string condition);

        //Delete all matching records from the database
        Task<int> DeleteAll(string condition);

        //Update the first record in the database
        Task<int> UpdateFirst(string[] columns, object[] values, string condition);

        //Update all matching records from the database
        Task<int> UpdateAll(string[] columns, object[] values, string condition);
    }
}