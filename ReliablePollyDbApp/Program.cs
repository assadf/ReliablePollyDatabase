using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Lib;
using Newtonsoft.Json;

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
                var conn = new SqlConnection("Server=localhost;Database=Sandpit;Trusted_Connection=True;");
                await conn.OpenAsync().ConfigureAwait(false);

                return conn;
            });

            var employee = await repo.GetEmployeeAsync(1).ConfigureAwait(false);

            Console.WriteLine(JsonConvert.SerializeObject(employee));
        }
    }
}
