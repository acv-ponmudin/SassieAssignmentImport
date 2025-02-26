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
        private static readonly int _batchSize = 50;
        //assignments count year wise
        //2025    121
        //2024    3708
        //2023    7723
        //2022    7223
        //2021    7537
        //2020    7049
        //2019    9753
        //2018    13799

        static async Task Main(string[] args)
        {
            try
            {
                CreateLogger();

                //List<JobImportResponse> jobImportResponses = new List<JobImportResponse>()
                //{
                //    new JobImportResponse(){AssignmentId=123,SurveyId="123",ClientLocationId="123",JobId="123"},
                //    new JobImportResponse(){AssignmentId=124,SurveyId="124",ClientLocationId="124",JobId="124"}
                //};
                //InsertSassieJob(jobImportResponses);

                //assignmentID = 26228303;
                //assignmentID = 23183043;
                //assignmentID = 22790360;
                //assignmentID = 26224623;//8 post-sale, no pre-sale 
                //assignmentID = 26224953;//acura 
                //assignmentID = 26224446;//with comments
                //assignmentID = 26224508;//to check images
                //assignmentID = 25312675;//repair order first page, non compliant items images 
                // 26224414 -> post-sale 15 and pre-sale 5 vehicles 

                bool authenticate = true;
                bool includeImages = true;

                var controller = new AssignmentImportController();
                Log.Information("Please wait while fetching assignments...");
                //var assignments = controller.GetAssignments();

                List<int> assignments = new List<int>() { 26224414 };//20115999, 18585686
                //List<int> assignments = new List<int>() { 25312618, 25312619, 25312620, 25312622, 25312623};
                //List<int> assignments = new List<int>() { 25312618, 25312619, 25312620, 25312622, 25312623, 25312624, 25312625, 25312626, 25312627, 25312628, 25312629, 25312630, 25312631, 25312632, 25312633, 25312634, 25312636, 25312637, 25312638, 25312639, 25312640, 25312642, 25312643, 25312646, 25312647, 25312649, 25312650, 25312651, 25312652, 25312653, 25312654, 25312655, 25312656, 25312657, 25312658, 25312659, 25312660, 25312661, 25312663, 25312664, 25312665, 25312666, 25312667, 25312668, 25312669, 25312671, 25312672, 25312673, 25312674, 25312675 };

                if (assignments == null || assignments.Count == 0)
                {
                    Log.Information($"No assignments to import!");
                    return;
                }

                Log.Information($"Total no. of assignments: {assignments.Count}");

                int iBatch = 0;

                for (int i = 0; i < assignments.Count; i += _batchSize)
                {
                    iBatch++;
                    // Get the current batch
                    List<int> batch = assignments.Skip(i).Take(_batchSize).ToList();

                    // Process the batch
                    Log.Information($"Batch-{iBatch} job import started.");
                    var jobImportResponses = await controller.ImportAssignmentsAsync(batch, authenticate, includeImages);

                    Log.Information($"Batch-{iBatch} job import completed.");

                    var success = jobImportResponses.Where(x => x.Status == System.Net.HttpStatusCode.Created).ToList();
                    var failed = jobImportResponses.Except(success).ToList();

                    // Update database
                    if (success != null && success.Count > 0)
                    {
                        controller.InsertSassieJob(success);
                        Log.Information($"Inserted [{success.Count}] successful job_ids from batch-{iBatch} into the database.");
                        //write jobImportResponses to file 
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(success, Newtonsoft.Json.Formatting.Indented);
                        //Log.Information($"SUCCESS JOB(S)::{Environment.NewLine}{json}");
                    }
                    else
                    {
                        Log.Information($"No successful jobs!");
                    }

                    if (failed != null && failed.Count > 0)
                    {
                        Log.Information($"Failed count [{failed.Count}].");
                        //write jobImportResponses to file 
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(failed, Newtonsoft.Json.Formatting.Indented);
                        Log.Information($"FAILED JOB(S)::{Environment.NewLine}{json}");
                    }
                    else
                    {
                        Log.Information($"No failed jobs!");
                    }

                    if (iBatch == 1) break;//TEST purpose
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "EXCEPTION!");
            }
            finally
            {
                Log.CloseAndFlush(); // Ensures logs are flushed
                Console.WriteLine($"DONE! Enter any key to exit!");
                Console.ReadKey();
            }
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
