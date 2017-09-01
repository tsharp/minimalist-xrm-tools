using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using DynamicsCrm.TestFramework.Factories;
using DynamicsCrm.TestFramework.Logging;

namespace DynamicsCrm.TestFramework.Core
{
    public static class DataFactory
    {
        private static void Process(StreamReader streamReader, IDataFactory factory)
        {
            using (CsvHelper.CsvReader reader = new CsvHelper.CsvReader(streamReader, new CsvHelper.Configuration.CsvConfiguration()
            {
                TrimFields = true,
                HasHeaderRecord = true
            }))
            {
                reader.ReadHeader();

                var dataTypes = ProcessHeaders(reader.FieldHeaders);
                while (reader.Read())
                {
                    factory.ProcessRow(dataTypes, reader);
                }
            }
        }

        private static Dictionary<string, Tuple<string, string>> ProcessHeaders(params string[] fields)
        {
            var results = new Dictionary<string, Tuple<string, string>>();

            foreach (var field in fields.Where(f => f != "ref" && f != "logicalName"))
            {
                var fieldSplit = field.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToList();
                results.Add(field, new Tuple<string, string>(fieldSplit[0], fieldSplit.Count > 1 ? fieldSplit[1] : string.Empty));
            }

            return results;
        }

        public static void CreateDefaultData(this CrmDb db)
        {
            ILog log = LogProvider.GetLogger("DataFactory");
            log.Info("Setting Up Default Data ...");
            db.CreateEntity("transactioncurrency", new Dictionary<string, object>() { { "isocurrencycode", "USD" } });
            db.CreateEntity("uom", new Dictionary<string, object>() { { "name", "Primary Unit" } });
            db.CreateEntity("uomschedule", new Dictionary<string, object>() { { "name", "Default Unit" } });
        }

        public static Entity CreateEntity(this CrmDb db, string logicalName, Dictionary<string, object> attribs)
        {
            Entity e = new Entity(logicalName);
            e.Attributes.AddRange(attribs);
            db.Add(e);
            return e;
        }

        public static IDataFactory LoadData(this IOrganizationService service, params string[] fileFilters)
        {
            ILog log = LogProvider.GetLogger("DataFactory");
            IDataFactory dataFac = new CsvDataFactory(service);

            var keys = ConfigurationManager.AppSettings.AllKeys.Where(k => k.StartsWith("testFramework.dataFile:")).OrderBy(k => k).ToArray();

            // Process Files
            foreach (var key in keys)
            {
                if (!File.Exists(ConfigurationManager.AppSettings[key]))
                {
                    log.Info($"{ConfigurationManager.AppSettings[key]} does not exist.");
                    continue;
                }

                using (var fileStream = File.OpenText(ConfigurationManager.AppSettings[key]))
                {
                    log.Info($"Loading Data File ... {ConfigurationManager.AppSettings[key]}");
                    if (fileStream.BaseStream.Length <= 0)
                    {
                        log.Info($"{ConfigurationManager.AppSettings[key]} was empty.");
                        continue;
                    }

                    Process(fileStream, dataFac);
                }
            }

            // Process Embedded Resources
            // var resources = Assembly.GetEntryAssembly().GetManifestResourceNames().Where(k => k.Contains(".TestFramework.") && k.EndsWith(".csv")).OrderBy(k => k).ToArray();

            var resourcesAsms = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                            a.GetManifestResourceNames().Where(k =>
                            k.Contains(".TestFramework.") &&
                            k.EndsWith(".csv")).Any())
                .Select(a =>
                    new
                    {
                        asm = a,
                        resources =
                        a.GetManifestResourceNames().Where(k =>
                            k.Contains(".TestFramework.") &&
                            k.EndsWith(".csv")).OrderBy(k => k).ToArray()
                    });

            foreach (var resourcesAsm in resourcesAsms)
            {
                foreach (var resource in resourcesAsm.resources)
                {
                    if(fileFilters != null && fileFilters.Any() && !fileFilters.Any(ff => resource.Contains(ff)))
                    {
                        continue;
                    }

                    log.Info($"Loading Data Resource Stream ... {resource}");
                    using (var resourceStream = resourcesAsm.asm.GetManifestResourceStream(resource))
                    {
                        if (resourceStream.Length <= 0)
                        {
                            log.Info($"{resource} was empty.");
                            continue;
                        }

                        using (StreamReader reader = new StreamReader(resourceStream))
                        {
                            Process(reader, dataFac);
                        }
                    }
                }
            }

            return dataFac;
        }
    }
}
