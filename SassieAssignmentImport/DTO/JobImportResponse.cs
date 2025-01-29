using Newtonsoft.Json;
using System.Net;

namespace SassieAssignmentImport.DTO
{
    internal class JobImportResponse
    {
        [JsonProperty("assignment_id")]
        public int AssignmentId { get; set; }

        [JsonProperty("status")]
        public HttpStatusCode? Status { get; set; }

        [JsonProperty("survey_id")]
        public string SurveyId { get; set; }

        [JsonProperty("client_location_id")]
        public string ClientLocationId { get; set; }

        [JsonProperty("job_id")]
        public object JobId { get; set; }

    }
}
