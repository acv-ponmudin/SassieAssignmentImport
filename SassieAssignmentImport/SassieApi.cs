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
                response.EnsureSuccessStatusCode();
                string responseData = await response.Content.ReadAsStringAsync();

                response1 = JsonConvert.DeserializeObject<AuthenticationResponse>(responseData);
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
                response.EnsureSuccessStatusCode();

                string responseData = await response.Content.ReadAsStringAsync();

                response1 = JsonConvert.DeserializeObject<JobImportResponse>(responseData);
            }
            catch (Exception)
            {
                throw;
            }

            return response1;
        }

        //public void Authenticate(string jsonData)
        //{
        //    string url = "https://uat.sassieshop.com/2sgstrans/sapi/api/token";
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //    request.Method = "POST";
        //    request.ContentType = "application/json";
        //    request.Accept = "*/*";
        //    //request.UserAgent = "PostmanRuntime/7.42.0";
        //    request.UserAgent = "sgstrans/1.0";

        //    // Write the JSON data to the request stream
        //    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
        //    {
        //        writer.Write(jsonData);
        //    }

        //    try
        //    {
        //        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        //        {
        //            Console.WriteLine($"Status Code: {response.StatusCode}");

        //            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        //            {
        //                string responseText = reader.ReadToEnd();
        //                Console.WriteLine($"Response: {responseText}");
        //            }
        //        }
        //    }
        //    catch (WebException ex)
        //    {
        //        if (ex.Response is HttpWebResponse errorResponse)
        //        {
        //            Console.WriteLine($"Error: {errorResponse.StatusCode}");
        //            using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
        //            {
        //                string errorText = reader.ReadToEnd();
        //                Console.WriteLine($"Error Details: {errorText}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Exception: {ex.Message}");
        //        }
        //    }
        //}
    }
}
