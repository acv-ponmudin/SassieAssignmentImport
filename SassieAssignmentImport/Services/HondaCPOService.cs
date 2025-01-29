using Microsoft.ApplicationBlocks.Data;
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
            int year = 2024;
            string cmdText = $"SELECT A.Assignment_ID " +
                $"FROM ASSIGNMENT_AUDIT A " +
                $"LEFT JOIN ASSIGNMENT_JOB_SASSIE S ON A.Assignment_ID = S.Assignment_ID " +
                $"WHERE YEAR(audit_date) = {year} AND S.Assignment_ID IS NULL " +
                $"ORDER BY audit_date DESC";
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