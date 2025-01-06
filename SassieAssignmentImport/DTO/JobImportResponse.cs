using Newtonsoft.Json;
using System.Net;

namespace SassieAssignmentImport.DTO
{
    internal class JobImportResponse
    {
        public int AssignmentId { get; set; }

        public HttpStatusCode? Status { get; set; }
        
        [JsonProperty("job_import")]
        public JobImport JobImport { get; set; }
       
    }

    public class JobImport
    {
        [JsonProperty("survey_id")]
        public string SurveyId { get; set; }

        [JsonProperty("client_location_id")]
        public string ClientLocationId { get; set; }

        [JsonProperty("job_id")]
        public object JobId { get; set; }
    }
}
