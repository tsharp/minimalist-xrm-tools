using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicsCrm.PSTools
{
    internal static class Extensions
    {
        public static string GetSolutionFileName(string uniqueName, string version, bool isManaged, string managedPostfix = "_managed")
        {
            return string.Format("{0}_{1}{2}.zip", uniqueName, version.Replace('.', '_'), isManaged ? managedPostfix : string.Empty);
        }

        public static void ExportSolutionToDisk(this IOrganizationService service, string directory, string solutionName, bool isManaged, string managedPostfix = "_managed")
        {
            // var baseDirPath = ".\\Solutions";
            if (isManaged) { directory = Path.Combine(directory, "Managed"); }
            if (!Directory.Exists(directory)) throw new Exception("Directory Does Not Exist: " + directory);
            var version = service.GetSolutionVersion(solutionName);
            var fileName = GetSolutionFileName(solutionName, version, isManaged, managedPostfix);
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, service.ExportSolution(solutionName, isManaged));
        }
    }
}
