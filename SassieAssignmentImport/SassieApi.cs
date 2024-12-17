using Newtonsoft.Json;
using SassieAssignmentImport.DTO;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SassieAssignmentImport
{
    class SassieApi
    {
        private readonly HttpClient _client;

        public SassieApi()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://uat.sassieshop.com")
            };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("2sgstrans", "1.0"));
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest data)
        {
            AuthenticationResponse authResponse = null;
            try
            {
                string jsonData = JsonConvert.SerializeObject(data); // Serialize to JSON
                StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync("2sgstrans/sapi/api/token", content);
                //response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Sassie Authentication SUCCESS!");
                    authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(responseData);
                }
                else
                {
                    Log.Error($"Sassie Authentication ERROR! StatusCode: {response.StatusCode}, {Environment.NewLine} Response: {responseData}");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EXCEPTION!");
            }
            return authResponse;
        }

        public async Task<JobImportResponse> ImportJobAsync(JobImportRequest request)
        {
            JobImportResponse jobResponse = null;
            try
            {
                string jsonData = JsonConvert.SerializeObject(request.Data); // Serialize to JSON
                StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);

                HttpResponseMessage response = await _client.PostAsync("2sgstrans/sapi/api/job_import", content);
                //response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    jobResponse = JsonConvert.DeserializeObject<JobImportResponse>(responseData);
                    Log.Information($"Assignment ID: {request.AssignmentID} job import SUCCESS! Job ID: {jobResponse.JobImport.JobId}");
                    jobResponse.AssignmentId = request.AssignmentID;
                    jobResponse.Status = response.StatusCode;
                }
                else
                {
                    var error = JsonConvert.DeserializeObject<dynamic>(responseData);
                    Log.Error($"Assignment ID: {request.AssignmentID} job import ERROR! {error}");
                    jobResponse = new JobImportResponse
                    {
                        AssignmentId = request.AssignmentID,
                        Status = response.StatusCode,
                        JobImport = new JobImport
                        {
                            SurveyId = request.SurveyID,
                            ClientLocationId = request.ClientLocationID,
                            JobId = error
                        }
                    };
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Assignment ID: {request.AssignmentID} EXCEPTION!");
            }

            return jobResponse;
        }
    }
}
