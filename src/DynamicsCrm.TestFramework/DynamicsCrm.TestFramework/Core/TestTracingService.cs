using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicsCrm.TestFramework.Logging;

namespace DynamicsCrm.TestFramework.Core
{
    public class SerilogTracingService : ITracingService
    {
        ILog log = LogProvider.GetCurrentClassLogger();

        public void Trace(string format, params object[] args)
        {
            log.Debug(() => { return string.Format(format, args); });
        }
    }
}
