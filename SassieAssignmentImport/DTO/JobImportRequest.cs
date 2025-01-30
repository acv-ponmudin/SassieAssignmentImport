using System.Collections.Generic;

namespace SassieAssignmentImport.DTO
{
    internal class JobImportRequest
    {
        public int AssignmentID { get; set; }
        public string SurveyID { get; set; }
        public string ClientLocationID { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
