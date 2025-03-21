﻿using SassieAssignmentImport.Controllers;
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
                //25075732 -> Could not get content from provided url.
                //25075850, 25076971, non-compliant review status

                bool isProduction = false;
                bool authenticate = true;
                bool includeImages = true;

                var controller = new AssignmentImportController(isProduction);
                Log.Information("Please wait while fetching assignments...");
                var assignments = controller.GetAssignments();

                //var assignments = new List<int>() { 25075819, 25075850, 25076971, 25076844,  25076642, 25076691 };

                if (assignments == null || assignments.Count == 0)
                {
                    Log.Information($"No assignments to import!");
                    return;
                }

                Log.Information($"Total no. of assignments: {assignments.Count}, batch size: {_batchSize}");

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
