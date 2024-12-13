using Newtonsoft.Json;

namespace SassieAssignmentImport
{
    internal class JobImportResponse
    {
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
        public string JobId { get; set; }

        public int AssignmentId { get; set; }
    }
}
