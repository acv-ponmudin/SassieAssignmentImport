using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SassieAssignmentImport.DTO;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SassieAssignmentImport.Services
{
    class SassieApiService
    {
        private readonly HttpClient _client;

        public SassieApiService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://uat.sassieshop.com")
            };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("2sgstrans", "1.0"));
            _client.Timeout = TimeSpan.FromMinutes(60);
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest data)
        {
            AuthenticationResponse authResponse = null;
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

            return authResponse;
        }

        public async Task<JobImportResponse> ImportJobAsync(JobImportRequest request)
        {
            string jsonData = JsonConvert.SerializeObject(request.Data); // Serialize to JSON
            StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);

            HttpResponseMessage response = await _client.PostAsync("2sgstrans/sapi/api/job_import", content);
            //response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();

            JobImportResponse jobResponse;
            if (response.IsSuccessStatusCode)
            {
                //jobResponse = JsonConvert.DeserializeObject<JobImportResponse>(responseData);
                JObject job_import = (JObject)JObject.Parse(responseData)["job_import"];
                jobResponse = job_import.ToObject<JobImportResponse>();
                Log.Information($"Assignment ID: {request.AssignmentID} job import SUCCESS! Job ID: {jobResponse.JobId}");
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
                    SurveyId = request.SurveyID,
                    ClientLocationId = request.ClientLocationID,
                    JobId = error
                };
            }

            return jobResponse;
        }
    }
}
