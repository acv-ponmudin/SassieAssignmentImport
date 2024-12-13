using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SassieAssignmentImport
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            try
            {
                int assignmentID;
                assignmentID = 26228303;
                assignmentID = 23183043;
                assignmentID = 22790360;
                assignmentID = 26224623;//8 post-sale, no pre-sale 
                assignmentID = 26224953;//acura 
                assignmentID = 26224446;//with comments

                List<int> assignments = new List<int>() { assignmentID, 22790360, 23183043 };
                await new HondaCPOInspectionReport().ImportAssignmentsAsync(assignments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}");
            }

            Console.WriteLine($"Enter any key to exit!");
            Console.ReadKey();
        }

        
    }
}
