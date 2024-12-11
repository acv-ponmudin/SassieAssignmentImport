using Microsoft.ApplicationBlocks.Data;
using System;
using System.Data;
using System.Data.SqlClient;

namespace SassieAssignmentImport
{
    /// <summary>
    /// Honda CPO specific DB activities
    /// </summary>
    public class DBHondaCPO
    {
        public DataSet GetHondaCPOOCR(int assignment_id)
        {
            DataSet dsResult;

            try
            {
                string WebAppsConnection = "Server=acv-asi-production.cylznspjfdxr.us-east-1.rds.amazonaws.com;Database=INSPECTIONDB;User id=webapps;password=exception;";

                SqlParameter[] objParameter = new SqlParameter[3];
                objParameter[0] = new SqlParameter("@Assignment_ID", assignment_id);
                objParameter[1] = new SqlParameter("@Language_Code", "en");
                objParameter[2] = new SqlParameter("@Version_Request", 1);

                dsResult = SqlHelper.ExecuteDataset(WebAppsConnection, CommandType.StoredProcedure, "usp_UDA_OnlineConsultationReport_HondaCPO_Sassie", objParameter);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.ToString());
            }
            finally
            {
            }

            return dsResult;
        }
    }
}