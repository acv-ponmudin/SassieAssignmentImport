﻿using SassieAssignmentImport.DTO;
using SassieAssignmentImport.Services;
using SassieAssignmentImport.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SassieAssignmentImport.Controllers
{
    internal class AssignmentImportController
    {
        private readonly HondaCPOService _dbHondaCPO;
        private readonly SassieApiService _sassieApi;
        private List<Dictionary<int, string>> _presale_list = new List<Dictionary<int, string>>();
        private List<Dictionary<int, string>> _postsale_list = new List<Dictionary<int, string>>();
        private Dictionary<string, Dictionary<string, string>> _presale_vehicles = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> _postsale_vehicles = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> _inspectionData = new Dictionary<string, string>();
        private readonly string GRANT_TYPE = "client_credentials";
        private readonly string CLIENT_ID = "WSwDiUqqv5Q2InctWBHkWeTWmDmfiNJl";
        private readonly string CLIENT_SECRET = "62UEIr61r2FQc9xyvRn4PBdmRQ4gTPwa";

        public AssignmentImportController()
        {
            _sassieApi = new SassieApiService();
            _dbHondaCPO = new HondaCPOService();

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

        public async Task ImportAssignmentsAsync(List<int> assignments)
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
                    return;

                var importList = new List<Task<JobImportResponse>>();
                foreach (int assignmentID in assignments)
                {
                    importList.Add(ImportSingleAssignmentAsync(assignmentID, authResponse.AccessToken));
                }

                jobImportResponses = await Task.WhenAll(importList);
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
        }

        public async Task<JobImportResponse> ImportSingleAssignmentAsync(int assignmentID, string token)
        {
            JobImportResponse jobResponse = null;
            try
            {
                Log.Information($"Processing Assignment ID: {assignmentID}");

                var dsCPOData = _dbHondaCPO.GetHondaCPOOCR(assignmentID);

                var divisionCode = dsCPOData.Tables[0].Rows[0]["Division_Code"].ToString().Trim();
                //HONDA:: 1039
                //ACURA:: 1061
                string surveyID = divisionCode != "B" ? "1039" : "1061";
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

                var jobRequest = new JobImportRequest
                {
                    AssignmentID = assignmentID,
                    SurveyID = surveyID,
                    ClientLocationID = clientLocationID,
                    Data = _inspectionData,
                    Token = token
                };

                jobResponse = await _sassieApi.ImportJobAsync(jobRequest);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Assignment ID: {assignmentID} EXCEPTION!");
            }

            return jobResponse;
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
            string vin_num;
            Dictionary<int, string> q_mapping;
            int qid;
            int ind = 0;
            string value;
            foreach (var pair in _postsale_vehicles)
            {
                vin_num = pair.Key;
                q_mapping = _postsale_list[ind];

                _inspectionData.Add(q_mapping[ind], "Yes");

                foreach (var item in QuestionMapping.vehicle_detail)
                {
                    if (!q_mapping.ContainsKey(item.Key))
                    {
                        continue;
                    }

                    _inspectionData.Add(q_mapping[item.Key], pair.Value[item.Value].Trim());
                }

                foreach (DataRow row in dsCPOData.Tables[3].Rows)
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

                        _inspectionData.Add(QuestionMapping.comments_mapping[q_mapping[qid]], "comments_dummy_test");
                    }

                }
                ind++;
            }

        }

        private void PopulatePresaleQuestions(DataSet dsCPOData)
        {
            string vin_num;
            Dictionary<int, string> q_mapping;
            int qid;
            int ind = 0;
            string value;
            foreach (var pair in _presale_vehicles)
            {
                vin_num = pair.Key;

                q_mapping = _presale_list[ind];

                _inspectionData.Add(q_mapping[ind], "Yes");

                foreach (var item in QuestionMapping.vehicle_detail)
                {
                    if (!q_mapping.ContainsKey(item.Key))
                    {
                        continue;
                    }

                    _inspectionData.Add(q_mapping[item.Key], pair.Value[item.Value].Trim());
                }

                foreach (DataRow row in dsCPOData.Tables[5].Rows)
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

                        _inspectionData.Add(QuestionMapping.comments_mapping[q_mapping[qid]], "comments_dummy_test");
                    }
                }
                ind++;
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
                if (!QuestionMapping.facility_mapping.ContainsKey(qid))
                {
                    continue;
                }
                _inspectionData.Add(QuestionMapping.facility_mapping[qid], QuestionMapping.objective[qval].Trim());
            }
        }

        private string ChangeNotApplicableText(string value)
        {
            return value.ToLower().Equals("na") ? "N/A" : value;
        }
    }
}