using SassieAssignmentImport.DTO;
using SassieAssignmentImport.Services;
using SassieAssignmentImport.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SassieAssignmentImport.Controllers
{
    internal class AssignmentImportController
    {
        #region Members
        private static int _counter = 1;
        private readonly string GRANT_TYPE = "client_credentials";
        private readonly string CLIENT_ID = "WSwDiUqqv5Q2InctWBHkWeTWmDmfiNJl";
        private readonly string CLIENT_SECRET = "62UEIr61r2FQc9xyvRn4PBdmRQ4gTPwa";
        private readonly string IMAGE_ROOTPATH = "https://cpo.true360.com/FileServer-Images";
        private readonly string DOC_ROOTPATH = "https://cpo.true360.com/FileServer-Documents";
        private readonly HondaCPOService _hondaCPOService;
        private readonly SassieApiService _sassieApi;
        private Dictionary<string, Dictionary<string, string>> _presale_vehicles;
        private Dictionary<string, Dictionary<string, string>> _postsale_vehicles;
        private Dictionary<string, string> _inspectionData;
        private List<Dictionary<int, string>> _presale_list;
        private List<Dictionary<int, string>> _postsale_list;
        private List<Dictionary<string, string>> records = new List<Dictionary<string, string>>();
        #endregion

        #region Constructor
        public AssignmentImportController()
        {
            _hondaCPOService = new HondaCPOService();
            _sassieApi = new SassieApiService();

            _presale_list = new List<Dictionary<int, string>>();
            _postsale_list = new List<Dictionary<int, string>>();

            _presale_list.Add(QuestionMapping.presale_mappingA);
            _presale_list.Add(QuestionMapping.presale_mappingB);
            _presale_list.Add(QuestionMapping.presale_mappingC);
            _presale_list.Add(QuestionMapping.presale_mappingD);
            _presale_list.Add(QuestionMapping.presale_mappingE);
            _presale_list.Add(QuestionMapping.presale_mappingF);
            _presale_list.Add(QuestionMapping.presale_mappingG);
            _presale_list.Add(QuestionMapping.presale_mappingH);
            _presale_list.Add(QuestionMapping.presale_mappingI);
            _presale_list.Add(QuestionMapping.presale_mappingJ);

            _postsale_list.Add(QuestionMapping.postsale_mappingA);
            _postsale_list.Add(QuestionMapping.postsale_mappingB);
            _postsale_list.Add(QuestionMapping.postsale_mappingC);
            _postsale_list.Add(QuestionMapping.postsale_mappingD);
            _postsale_list.Add(QuestionMapping.postsale_mappingE);
            _postsale_list.Add(QuestionMapping.postsale_mappingF);
            _postsale_list.Add(QuestionMapping.postsale_mappingG);
            _postsale_list.Add(QuestionMapping.postsale_mappingH);
            _postsale_list.Add(QuestionMapping.postsale_mappingI);
            _postsale_list.Add(QuestionMapping.postsale_mappingJ);
        } 
        #endregion

        #region Public Methods
        public List<int> GetAssignments()
        {
            return _hondaCPOService.GetAssignments();
        }
        
        public async Task<List<JobImportResponse>> ImportAssignmentsAsync(List<int> assignments)
        {
            JobImportResponse[] jobImportResponses = null;
            try
            {
                Log.Information("Sassie Authentication In-Progress...");
                var authRequest = new AuthenticationRequest
                {
                    GrantType = GRANT_TYPE,
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET
                };
                var authResponse = await _sassieApi.AuthenticateAsync(authRequest);

                if (authResponse == null)
                    return null;

                //Asynchronous for I/O bound
                var importList = new List<Task<JobImportResponse>>();
                foreach (int assignmentID in assignments)
                {
                    importList.Add(ImportSingleAssignmentAsync(assignmentID));
                }
                jobImportResponses = await Task.WhenAll(importList);

                //CreateCSV();
            }
            finally
            {
                //write jobImportResponses to file 
                if (jobImportResponses != null && jobImportResponses.Length > 0)
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(jobImportResponses, Newtonsoft.Json.Formatting.Indented);
                    Log.Information($"RESULT::{Environment.NewLine}{json}");
                }
            }

            return jobImportResponses?.ToList();
        }

        public void InsertSassieJob(List<JobImportResponse> jobImportResponses)
        {
            // Convert List to XML
            XElement root = new XElement("Root",
                jobImportResponses.Select(emp =>
                    new XElement("Job",
                        new XElement("assignment_id", emp.AssignmentId),
                        new XElement("survey_id", emp.SurveyId),
                        new XElement("client_location_id", emp.ClientLocationId),
                        new XElement("job_id", emp.JobId)
                    )
                )
            );

            _hondaCPOService.InsertSassieJob(root.ToString());
        }

        #endregion

        #region Private Methods
        private async Task<JobImportResponse> ImportSingleAssignmentAsync(int assignmentID)
        {
            JobImportResponse jobResponse;
            try
            {
                Log.Information($"{_counter++}) Processing Assignment ID: {assignmentID}");

                var dsCPOData = _hondaCPOService.GetHondaCPOOCR(assignmentID);

                var divisionCode = dsCPOData.Tables[0].Rows[0]["Division_Code"].ToString().Trim();
                //HONDA:: 1039
                //ACURA:: 1061
                string surveyID = divisionCode == "A" ? "1039" : "1061";
                string clientLocationID = dsCPOData.Tables[0].Rows[0]["Dealer_Code"].ToString().Trim();

                //1. Consultation information 
                //2. Dealer information 
                //3. Dealer contact information 
                //4. Inspection summary 
                //5. Vehicle compliance findings 
                //6. Post-sale (Documentation inspection only)
                //7. Pre-sale (Documentation and Vehicle inspection)
                //8. Facility inspection 
                //9. Facility images 

                _inspectionData = new Dictionary<string, string>() {
                    {"survey_id", surveyID },
                    {"client_location_id", clientLocationID }
                };

                ConsultationInformation(dsCPOData);
                DealerInformation(dsCPOData);
                PopulateVehicles(dsCPOData);
                PopulatePostsaleQuestions(dsCPOData);
                PopulatePresaleQuestions(dsCPOData);
                FacilityInspection(dsCPOData);

                //records.Add(_inspectionData);
                //return new JobImportResponse();

                var jobRequest = new JobImportRequest
                {
                    AssignmentID = assignmentID,
                    SurveyID = surveyID,
                    ClientLocationID = clientLocationID,
                    Data = _inspectionData,
                };

                jobResponse = await _sassieApi.ImportJobAsync(jobRequest);
                //jobResponse = new JobImportResponse();
                //await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Assignment ID: {assignmentID} EXCEPTION!");
                jobResponse = new JobImportResponse { AssignmentId = assignmentID };
            }

            return jobResponse;
        }
        
        private void CreateCSV()
        {

            // Get all unique keys across all dictionaries
            var allKeys = records.SelectMany(dict => dict.Keys).Distinct().ToList();

            // Specify CSV file path
            string csvFilePath = "output.csv";

            // Write to CSV
            using (var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
            {
                // Write the header
                writer.WriteLine(string.Join(",", allKeys));

                // Write each row
                foreach (var record in records)
                {
                    var row = allKeys.Select(key => record.ContainsKey(key) ? record[key]?.ToString() : "").ToArray();
                    writer.WriteLine(string.Join(",", row));
                }
            }

            Console.WriteLine($"CSV file '{csvFilePath}' created successfully!");
        }

        private void ConsultationInformation(DataSet dsCPOData)
        {

            foreach (var item in QuestionMapping.consultation_mapping)
            {
                _inspectionData.Add(item.Value, dsCPOData.Tables[0].Rows[0][item.Key].ToString());
            }

            _inspectionData.Add("question_1", Convert.ToDateTime(dsCPOData.Tables[0].Rows[0]["Audit_Date"]).ToString("yyyy-MM-dd"));
            _inspectionData.Add("question_21", Convert.ToDateTime(dsCPOData.Tables[0].Rows[0]["Audit_Date"]).ToShortTimeString());

        }

        private void DealerInformation(DataSet dsCPOData)
        {
            foreach (var item in QuestionMapping.dealer_mapping)
            {
                _inspectionData.Add(item.Value, dsCPOData.Tables[0].Rows[0][item.Key].ToString());
            }

        }

        private void PopulateVehicles(DataSet dsCPOData)
        {
            string vin;
            Dictionary<string, string> detail;
            _presale_vehicles = new Dictionary<string, Dictionary<string, string>>();
            _postsale_vehicles = new Dictionary<string, Dictionary<string, string>>();
            foreach (DataRow item in dsCPOData.Tables[1].Rows)
            {
                vin = item["Vehicle_VIN"].ToString();
                detail = new Dictionary<string, string>()
                    {
                         {"VIN",item["Vehicle_VIN"].ToString() },
                         {"Manufacturer","" },
                        {"Make_Description",item["Make_Description"].ToString() },
                        {"Model_Description", item["Model_Description"].ToString()},
                        {"Vehicle_Year", item["Vehicle_Year"].ToString() },
                        {"Stock_Number", item["Stock_ID"].ToString() },
                        {"Tier", item["Tier_Data"].ToString() },
                    };

                if (item["Audit_Type"].Equals("Pre"))
                {
                    _presale_vehicles.Add(vin, detail);
                }
                else
                {
                    _postsale_vehicles.Add(vin, detail);
                }
            }
        }

        private void PopulatePostsaleQuestions(DataSet dsCPOData)
        {
            int ind = 0;
            string vin_num;
            Dictionary<int, string> q_mapping;

            foreach (var pair in _postsale_vehicles)
            {
                vin_num = pair.Key;
                q_mapping = _postsale_list[ind];

                _inspectionData.Add(q_mapping[ind], "Yes");

                //Vehicle detail
                AddVehicleDetail(q_mapping, pair.Value);

                //Consultation
                AddConsultationData(dsCPOData, 3, vin_num, q_mapping);//tableIndex=3 for Post-sale data 

                //Documents
                AddPostsaleDocuments(vin_num, dsCPOData, q_mapping);

                ind++;
            }

        }

        private void PopulatePresaleQuestions(DataSet dsCPOData)
        {
            int ind = 0;
            string vin_num;
            Dictionary<int, string> q_mapping;

            foreach (var pair in _presale_vehicles)
            {
                vin_num = pair.Key;
                q_mapping = _presale_list[ind];

                _inspectionData.Add(q_mapping[ind], "Yes");

                //Vehicle detail
                AddVehicleDetail(q_mapping, pair.Value);

                //Consultation
                AddConsultationData(dsCPOData, 5, vin_num, q_mapping);//tableIndex=5 for Pre-sale data 

                //Pre-sale Images
                AddPresaleImages(vin_num, dsCPOData, q_mapping);

                //Documents
                AddPresaleDocuments(vin_num, dsCPOData, q_mapping);

                ind++;
            }
        }

        private void AddVehicleDetail(Dictionary<int, string> q_mapping, Dictionary<string, string> pairValue)
        {
            foreach (var item in QuestionMapping.vehicle_detail)
            {
                if (!q_mapping.ContainsKey(item.Key))
                {
                    continue;
                }

                _inspectionData.Add(q_mapping[item.Key], pairValue[item.Value].Trim());
            }
        }

        private void AddConsultationData(DataSet dsCPOData, int tableIndex, string vin_num, Dictionary<int, string> q_mapping)
        {
            int qid;
            string value, comments;

            foreach (DataRow row in dsCPOData.Tables[tableIndex].Rows)
            {
                qid = (int)row["Question_ID"];
                value = row[vin_num].ToString().Trim();
                if (!q_mapping.ContainsKey(qid))
                {
                    continue;
                }
                value = ChangeNotApplicableText(value);

                _inspectionData.Add(q_mapping[qid], value);

                if (!value.ToLower().Equals("yes"))
                {
                    if (!QuestionMapping.comments_mapping.ContainsKey(q_mapping[qid]))
                    {
                        throw new Exception(string.Format("comments question missing for {0}!!", q_mapping[qid]));
                    }
                    comments = GetComments(vin_num, qid, dsCPOData);
                    _inspectionData.Add(QuestionMapping.comments_mapping[q_mapping[qid]], comments);
                }
            }
        }

        private void AddPresaleImages(string vin_num, DataSet dsCPOData, Dictionary<int, string> q_mapping)
        {
            int imgNum;
            string imgFile;

            string rowFilter = $"Vehicle_VIN = '{vin_num}'";
            DataRow[] matchRows = dsCPOData.Tables[4].Select(rowFilter);
            if (matchRows == null)
            {
                return;
            }

            foreach (DataRow row in matchRows)
            {
                imgNum = (int)row["Image_SeqNumber"];
                if (!q_mapping.ContainsKey(imgNum))
                {
                    continue;
                }

                imgFile = $"{IMAGE_ROOTPATH}{row["ImageFile"].ToString().Replace("\\", "/")}";

                _inspectionData.Add(q_mapping[imgNum], imgFile);
            }
        }

        private void AddPresaleDocuments(string vin_num, DataSet dsCPOData, Dictionary<int, string> q_mapping)
        {
            int docType;
            string docFile;

            string rowFilter = $"Vehicle_VIN = '{vin_num}'";
            DataRow[] matchRows = dsCPOData.Tables[10].Select(rowFilter);
            if (matchRows == null)
            {
                return;
            }

            foreach (DataRow row in matchRows)
            {
                docType = ((int)row["Document_Type"]) * 100;//To keep the dict key unique
                if (!q_mapping.ContainsKey(docType))
                {
                    continue;
                }

                docFile = $"{DOC_ROOTPATH}{row["DocumentFile"].ToString().Replace("\\", "/")}";

                _inspectionData.Add(q_mapping[docType], docFile);
            }
        }

        private void AddPostsaleDocuments(string vin_num, DataSet dsCPOData, Dictionary<int, string> q_mapping)
        {
            int docType;
            string docFile;

            string rowFilter = $"Vehicle_VIN = '{vin_num}'";
            DataRow[] matchRows = dsCPOData.Tables[11].Select(rowFilter);
            if (matchRows == null)
            {
                return;
            }

            foreach (DataRow row in matchRows)
            {
                docType = ((int)row["Document_Type"]) * 100;//To keep the dict key unique
                if (!q_mapping.ContainsKey(docType))
                {
                    continue;
                }

                docFile = $"{DOC_ROOTPATH}{row["DocumentFile"].ToString().Replace("\\", "/")}";

                _inspectionData.Add(q_mapping[docType], docFile);
            }
        }

        private void FacilityInspection(DataSet dsCPOData)
        {
            int qid;
            int qval;
            foreach (DataRow row in dsCPOData.Tables[6].Rows)
            {
                qid = (int)row["Question_ID"];
                qval = (int)row["Question_Value"];
                if (!QuestionMapping.facility_mapping.ContainsKey(qid) || !QuestionMapping.objective.ContainsKey(qval))
                {
                    continue;
                }
                _inspectionData.Add(QuestionMapping.facility_mapping[qid], QuestionMapping.objective[qval].Trim());
            }

            AddFacilityImages(dsCPOData);
        }

        private void AddFacilityImages(DataSet dsCPOData)
        {
            int imgNum;
            string imgFile;

            foreach (DataRow row in dsCPOData.Tables[7].Rows)
            {
                imgNum = (int)row["Image_SeqNumber"];
                if (!QuestionMapping.facility_mapping.ContainsKey(imgNum))
                {
                    continue;
                }

                imgFile = $"{IMAGE_ROOTPATH}{row["ImageFile"].ToString().Replace("\\", "/")}";

                _inspectionData.Add(QuestionMapping.facility_mapping[imgNum], imgFile);
            }
        }

        private string ChangeNotApplicableText(string value)
        {
            return value.ToLower().Equals("na") ? "N/A" : value;
        }

        private string GetComments(string vin_num, int qid, DataSet dsCPOData)
        {
            string rowFilter = $"Vehicle_VIN = '{vin_num}' AND Question_ID = {qid}";
            DataRow[] matchRows = dsCPOData.Tables[8].Select(rowFilter);
            string comments = "";
            if (matchRows != null && matchRows.Length > 0)
            {
                comments = matchRows[0]["Question_Comments"].ToString();
            }

            return comments;
        }
        #endregion
    }
}
