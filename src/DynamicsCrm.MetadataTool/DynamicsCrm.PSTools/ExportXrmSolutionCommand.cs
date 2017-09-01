using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace DynamicsCrm.PSTools
{
    [Cmdlet(VerbsData.Export, "XrmSolution")]
    public class ExportXrmSolutionCommand : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public Microsoft.Xrm.Tooling.Connector.CrmServiceClient Connection { get; set; }

        [Parameter(Mandatory = true)]
        public string Directory { get; set; }

        [Parameter(Mandatory = true)]
        public string SolutionName { get; set; }

        [Parameter(Mandatory = true)]
        public bool Managed { get; set; }

        [Parameter(Mandatory = false)]
        public string ManagedPostfix { get; set; } = "_managed";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            Connection.ExportSolutionToDisk(Directory, SolutionName, Managed, ManagedPostfix);
        }
    }
}
