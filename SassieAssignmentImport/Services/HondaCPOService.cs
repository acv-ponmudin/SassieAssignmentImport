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

        public List<int> GetAssignments()
        {
            //Fetching only honda assignments

            int year = 2023;

            string cmdText = $"SELECT A.Assignment_ID FROM ASSIGNMENT_AUDIT A \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tLEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tWHERE ORO.Account_Number LIKE '20%' \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND LEN(ORO.Account_Number) = 6 \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND YEAR(A.audit_date) = {year} \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND S.Assignment_ID IS NULL \r\n\t\t\t\t\t\t\t\t\t\t\t\tORDER BY A.audit_date DESC";

            //string cmdText = $"SELECT top 100 A.Assignment_ID FROM ASSIGNMENT_AUDIT A \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tLEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tWHERE ORO.Account_Number LIKE '20%' \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND LEN(ORO.Account_Number) = 6 \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND YEAR(A.audit_date) = {year} \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND S.Assignment_ID IS NULL \r\n\t\t\t\t\t\t\t\t\t\t\t\tORDER BY A.audit_date DESC";

            //var dtStart = new DateTime(2022, 6, 20).ToString("yyyy-MM-dd");
            //var dtEnd = new DateTime(2022, 6, 22).ToString("yyyy-MM-dd");
            //string cmdText = $"SELECT A.Assignment_ID FROM ASSIGNMENT_AUDIT A \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION AS O ON A.Dealer_ID = O.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tINNER JOIN dbo.ORGANIZATION_RELATIONSHIP_ROLE AS ORO ON O.Organization_ID = ORO.Organization_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tLEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID \r\n\t\t\t\t\t\t\t\t\t\t\t\tWHERE ORO.Account_Number LIKE '20%' \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND LEN(ORO.Account_Number) = 6 \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND A.audit_date between '{dtStart}' and '{dtEnd}' \r\n\t\t\t\t\t\t\t\t\t\t\t\tAND S.Assignment_ID IS NULL \r\n\t\t\t\t\t\t\t\t\t\t\t\tORDER BY A.audit_date DESC";

            DataSet dsResult = SqlHelper.ExecuteDataset(_connection, CommandType.Text, cmdText);

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