using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SassieAssignmentImport
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            // Initialize Serilog with file sink
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()      // Also logs to console
                .WriteTo.File(
                    path: "logs/log-.txt", // File path with rolling log
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            Log.Information("Please wait while initializing...");
            try
            {
                int assignmentID;
                assignmentID = 26228303;
                assignmentID = 23183043;
                assignmentID = 22790360;
                assignmentID = 26224623;//8 post-sale, no pre-sale 
                assignmentID = 26224953;//acura 
                assignmentID = 26224446;//with comments

                List<int> assignments = new List<int>() { assignmentID , 22790360 };
                await new HondaCPOInspectionReport().ImportAssignmentsAsync(assignments);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "EXCEPTION!");
            }
            finally
            {
                Log.CloseAndFlush(); // Ensures logs are flushed
            }

            Console.WriteLine($"Enter any key to exit!");
            Console.ReadKey();
        }

        
    }
}
