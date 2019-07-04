using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Lib;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;

namespace ReliablePollyDbApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Started...");

            try
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            Console.WriteLine("Finished!");
            Console.ReadLine();
        }

        public static async Task MainAsync(string[] args)
        {
            var repo = new EmployeeSqlRepository(async () =>
            {
                var conn = new PollySqlConnection(
                    "Server=localhost;Database=Sandpit;Trusted_Connection=True",
                    GetStandardDatabaseAsyncPolicies(),
                    GetStandardDatabaseSyncPolicies());

                await conn.OpenAsync().ConfigureAwait(false);

                return conn;
            });

            var employee = await repo.GetEmployeeAsync(1).ConfigureAwait(false);

            Console.WriteLine(JsonConvert.SerializeObject(employee));
        }

        public static IAsyncPolicy[] GetStandardDatabaseAsyncPolicies()
        {
            IAsyncPolicy[] policies =
            {
                Policy
                    .Handle<SqlException>()
                    .Or<TimeoutRejectedException>()
                    .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(1000)),
                //Policy.Handle<TimeoutRejectedException>().WaitAndRetryAsync(1, i => TimeSpan.FromMilliseconds(1000)),
                Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic)
            };

            return policies;
        }

        public static ISyncPolicy[] GetStandardDatabaseSyncPolicies()
        {
            ISyncPolicy[] policies =
            {
                Policy.Handle<SqlException>().WaitAndRetry(3, i => TimeSpan.FromMilliseconds(1000))
            };

            return policies;
        }
    }
}
