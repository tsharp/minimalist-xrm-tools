using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicsCrm.TestFramework.Core
{
    public static class TracingServiceFactory
    {
        public static ITracingService GetTracingService(IOrganizationService service, TestContext context)
        {
            return new SerilogTracingService();
        }
    }
}
