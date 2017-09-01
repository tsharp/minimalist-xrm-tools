using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace DynamicsCrm.PSTools
{
    /// <summary>
    /// Converts a specialized csv file to crm schema.
    /// </summary>
    [Cmdlet(VerbsData.Import, "XrmSchema")]
    class ImportSchemaCommand : Cmdlet
    {
    }
}
