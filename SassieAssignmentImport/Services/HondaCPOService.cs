using Microsoft.ApplicationBlocks.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SassieAssignmentImport.Services
{
    /// <summary>
    /// Honda CPO specific DB activities
    /// </summary>
    public class HondaCPOService
    {
        private readonly string _connection = "Server=acv-asi-production.cylznspjfdxr.us-east-1.rds.amazonaws.com;Database=INSPECTIONDB;User id=webapps;password=exception;";

        public IList<int> GetAssignments()
        {
            int year = 2023;
            //Honda Account Number starts with '20'
            //Acura Account Number starts with '25'
            int hondaAcctNumber = 20;
            int acuraAcctNumber = 25;
            var startDate = new DateTime(2022, 6, 20).ToString("yyyy-MM-dd");
            var endDate = new DateTime(2022, 6, 22).ToString("yyyy-MM-dd");

            //Query by Year
            string cmdText1 = $"SELECT A.Assignment_ID FROM ASSIGNMENT_AUDIT A                                                          INNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID                                                     INNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID                             LEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID                                                      WHERE (ORO.Account_Number LIKE '{hondaAcctNumber}%' or ORO.Account_Number LIKE '{acuraAcctNumber}%')                        AND LEN(ORO.Account_Number) = 6 AND YEAR(A.audit_date) = {year} AND S.Assignment_ID IS NULL                             ORDER BY A.audit_date DESC";

            //Query between dates
            string cmdText3 = $"SELECT A.Assignment_ID FROM ASSIGNMENT_AUDIT A                                                          INNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID                                                     INNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID                             LEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID                                                  WHERE (ORO.Account_Number LIKE '{hondaAcctNumber}%' or ORO.Account_Number LIKE '{acuraAcctNumber}%')  AND LEN(ORO.Account_Number) = 6  AND A.audit_date between '{startDate}' and '{endDate}'  AND S.Assignment_ID IS NULL                ORDER BY A.audit_date DESC";

            //Query between dates NOT joins ASSIGNMENT_JOB_SASSIE- Temp
            string cmdText4 = $"SELECT A.Assignment_ID FROM ASSIGNMENT_AUDIT A                                                           INNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID                                                     INNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID                         WHERE (ORO.Account_Number LIKE '{hondaAcctNumber}%' or ORO.Account_Number LIKE '{acuraAcctNumber}%')                        AND LEN(ORO.Account_Number) = 6                                                                                             AND A.audit_date between '{startDate}' and '{endDate}'                                                                      ORDER BY A.audit_date DESC";

            DataSet dsResult = SqlHelper.ExecuteDataset(_connection, CommandType.Text, cmdText4);

            List<int> res = dsResult.Tables[0].AsEnumerable().Select(row => (int)row["Assignment_ID"]).ToList();
            return res;
        }

        public DataSet GetHondaCPOOCR(int assignment_id)
        {
            SqlParameter[] objParameter = new SqlParameter[3];
            objParameter[0] = new SqlParameter("@Assignment_ID", assignment_id);
            objParameter[1] = new SqlParameter("@Language_Code", "en");
            objParameter[2] = new SqlParameter("@Version_Request", 1);

            DataSet dsResult = SqlHelper.ExecuteDataset(_connection, CommandType.StoredProcedure, "usp_UDA_OnlineConsultationReport_HondaCPO_Sassie", objParameter);

            return dsResult;
        }

        public void InsertSassieJob(string xml)
        {
            SqlParameter[] objParameter = new SqlParameter[1];
            objParameter[0] = new SqlParameter("@XmlData", SqlDbType.Xml) { Value = xml };
            _ = SqlHelper.ExecuteNonQuery(_connection, CommandType.StoredProcedure, "USP_UDA_INSERT_ASSIGNMENT_JOB_SASSIE", objParameter);
        }
    }
}