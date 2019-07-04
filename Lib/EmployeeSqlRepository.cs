using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Lib
{
    public class EmployeeSqlRepository
    {
        private readonly Func<Task<IDbConnection>> _dbConnectionFactory;

        public EmployeeSqlRepository(Func<Task<IDbConnection>> dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Employee> GetEmployeeAsync(int id)
        {
            using (var conn = await _dbConnectionFactory().ConfigureAwait(false))
            {
                //return await conn.QueryFirstOrDefaultAsync<Employee>("WAITFOR DELAY '00:00:10'; Select 1 as ID").ConfigureAwait(false);
                return await conn.QueryFirstOrDefaultAsync<Employee>("THROW 50000, 'Oops, Some Error!!!', 1").ConfigureAwait(false);
            }
        }

        public async Task InsertEmployeeAsync()
        {

        }
    }
}
