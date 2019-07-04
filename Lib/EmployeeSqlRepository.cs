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
            using (var dbConnection = await _dbConnectionFactory().ConfigureAwait(false))
            {
                await dbConnection.ExecuteAsync("THROW 50001, 'Oops Error Occurred!!!', 1").ConfigureAwait(false);
                var employee = await dbConnection.QueryFirstOrDefaultAsync<Employee>("SELECT 1 as ID, 'Joe' as FirstName, 'Blog' as LastName, 100 as DepartmentID").ConfigureAwait(false);

                return employee;
            }
        }
    }
}
