using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace DynamicsCrm.PSTools
{
    [RunInstaller(true)]
    public class PSToolsInstaller : PSSnapIn
    {
        public override string Description
        {
            get
            {
                return "DynamicsCrm PowerShell Tools";
            }
        }

        public override string Name
        {
            get
            {
                return "DynamicsCrm.PSTools";
            }
        }

        public override string Vendor
        {
            get
            {
                return "CRM Ninjas";
            }
        }
    }
}
