using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            AuthenticationResponse response1;
            try
            {
                string jsonData = JsonConvert.SerializeObject(data); // Serialize to JSON
                StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync("2sgstrans/sapi/api/token", content);
                //response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    response1 = JsonConvert.DeserializeObject<AuthenticationResponse>(responseData);
                }
                else
                {
                    //Console.WriteLine($"ERROR: Assignment ID: {assignmentID}, StatusCode: {response.StatusCode}");
                    //Console.WriteLine($"Response: {responseData}");
                    var error = $"ERROR: StatusCode: {response.StatusCode}, {Environment.NewLine} Response: {responseData}";
                    throw new Exception(error);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return response1;
        }

        public async Task<JobImportResponse> ImportJobAsync(Dictionary<string, string> data, string token)
        {
            JobImportResponse response1;
            try
            {
                string jsonData = JsonConvert.SerializeObject(data); // Serialize to JSON
                StringContent content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await _client.PostAsync("2sgstrans/sapi/api/job_import", content);
                //response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    response1 = JsonConvert.DeserializeObject<JobImportResponse>(responseData);
                }
                else
                {
                    //Console.WriteLine($"ERROR: Assignment ID: {assignmentID}, StatusCode: {response.StatusCode}");
                    //Console.WriteLine($"Response: {responseData}");
                    var error = $"ERROR: StatusCode: {response.StatusCode}, {Environment.NewLine} Response: {responseData}";
                    throw new Exception(error);
                }

            }
            catch (Exception)
            {

                throw;
            }

            return response1;
        }
    }
}
