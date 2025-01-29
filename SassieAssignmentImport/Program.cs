using SassieAssignmentImport.Controllers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SassieAssignmentImport
{
    internal class Program
    {
        private static int _batchSize = 100;

        static async Task Main(string[] args)
        {
            try
            {
                CreateLogger();
                Log.Information("Please wait while initializing...");

                //2025    121
                //2024    3708
                //2023    7723
                //2022    7223
                //2021    7537
                //2020    7049
                //2019    9753
                //2018    13799

                int assignmentID;
                assignmentID = 26228303;
                assignmentID = 23183043;
                assignmentID = 22790360;
                assignmentID = 26224623;//8 post-sale, no pre-sale 
                assignmentID = 26224953;//acura 
                assignmentID = 26224446;//with comments
                assignmentID = 26224508;//to check images
                assignmentID = 25312675;//repair order first page, non compliant items images 

                //List<JobImportResponse> jobImportResponses = new List<JobImportResponse>()
                //{
                //    new JobImportResponse(){AssignmentId=123,SurveyId="123",ClientLocationId="123",JobId="123"},
                //    new JobImportResponse(){AssignmentId=124,SurveyId="124",ClientLocationId="124",JobId="124"}
                //};
                //InsertSassieJob(jobImportResponses);

                //List<int> assignments = new List<int>() { 25312618 };
                //List<int> assignments = new List<int>() { 25312618, 25312619, 25312620, 25312622, 25312623, 25312624, 25312625, 25312626, 25312627, 25312628, 25312629, 25312630, 25312631, 25312632, 25312633, 25312634, 25312636, 25312637, 25312638, 25312639, 25312640, 25312642, 25312643, 25312646, 25312647, 25312649, 25312650, 25312651, 25312652, 25312653, 25312654, 25312655, 25312656, 25312657, 25312658, 25312659, 25312660, 25312661, 25312663, 25312664, 25312665, 25312666, 25312667, 25312668, 25312669, 25312671, 25312672, 25312673, 25312674, 25312675 };

                var controller = new AssignmentImportController();
                var assignments = controller.GetAssignments();

                for (int i = 0; i < assignments.Count; i += _batchSize)
                {
                    // Get the current batch
                    List<int> batch = assignments.Skip(i).Take(_batchSize).ToList();

                    // Process the batch
                    var success = await controller.ImportAssignmentsAsync(batch);

                    // Update database
                    if (success.Count > 0)
                    {
                        controller.InsertSassieJob(success);
                    }
                }

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

        static void CreateLogger()
        {
            // Initialize Serilog with file sink
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()      // Also logs to console
                .WriteTo.File(
                    path: "logs/log-.txt", // File path with rolling log
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

    }
}
