using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using DynamicsCrm.TestFramework.Logging;

namespace DynamicsCrm.TestFramework.Factories
{
    public class CsvDataFactory : IDataFactory
    {
        private Dictionary<string, EntityReference> lookups = new Dictionary<string, EntityReference>();
        public CsvDataFactory(IOrganizationService service) : base(service)
        {
        }

        protected EntityReference Lookup(string logicalName, string name)
        {
            var lookupKey = string.Format("{0}::{1}", logicalName, name);
            if (lookups.ContainsKey(lookupKey)) return lookups[lookupKey];

            QueryExpression query = new QueryExpression(logicalName);
            query.Criteria.AddCondition("name", ConditionOperator.Equal, name);

            var result = service.RetrieveMultiple(query).Entities.First().ToEntityReference();

            lookups.Add(lookupKey, result);

            log.Debug($"Found {logicalName}:{name}:{result.Id}");

            return result;
        }

        protected string LoadFile(string filename)
        {
            if (File.Exists(filename))
            {
                return File.ReadAllText(filename);
            }
            else
            {
                log.Warn($"Failed loading {filename}. This is referenced in the source data file.");
            }

            return null;
        }
        
        protected override Entity ProcessRowData(Dictionary<string, Tuple<string, string>> dataTypes, CsvReader reader)
        {
            // Skip if ref is null.
            if (string.IsNullOrEmpty(reader.GetField("ref")))
            {
                log.Info("Row Skipped, ref field was empty or not present.");
                return null;
            }

            Entity entity = new Entity(reader.GetField("logicalName"));
            log.Info($"Processing Row ... {reader.GetField("ref")}:{entity.LogicalName}");

            foreach (var field in dataTypes)
            {
                object data = null;

                var sdata = reader.GetField(field.Key);
                if (sdata == null || string.IsNullOrEmpty(sdata)) continue;

                switch (field.Value.Item2)
                {
                    case "file":
                        data = LoadFile(sdata);
                        break;
                    case "bool":
                        data = reader.GetField<bool>(field.Key);
                        break;
                    case "lookup":
                        data = Lookup(field.Key.Split(':')[2], sdata);
                        break;
                    case "ref":
                        data = GetRef(reader.GetField<decimal>(field.Key));
                        break;
                    case "optionset":
                        data = new OptionSetValue(reader.GetField<int>(field.Key));
                        break;
                    case "double":
                        data = reader.GetField<double>(field.Key);
                        break;
                    case "float":
                        data = reader.GetField<float>(field.Key);
                        break;
                    case "int":
                        data = reader.GetField<int>(field.Key);
                        break;
                    case "decimal":
                        data = reader.GetField<decimal>(field.Key);
                        break;
                    case "money":
                        data = new Money(reader.GetField<decimal>(field.Key));
                        break;
                    case "eref":
                        var split = reader.GetField<string>(field.Key).Split(':');
                        data = new EntityReference(split[0], new Guid(split[1]));
                        break;
                    default:
                        data = sdata;
                        break;
                }

                if(field.Value.Item1 == "transactioncurrencyid")
                {
                    data = Currencies[sdata];
                }

                if (data != null)
                {
                    var fieldName = field.Value.Item1;
                    if (field.Value.Item1.StartsWith("address_"))
                    {
                        switch(entity.LogicalName)
                        {
                            case "account":
                            case "contact":
                                fieldName = fieldName.Replace("address_", "address1_");
                                break;
                            case "salesorder":
                            case "invoice":
                            case "quote":
                            case "salesorderdetail":
                            case "invoicedetail":
                            case "quotedetail":
                                fieldName = fieldName.Replace("address_", "shipto_");
                                break;
                            default:
                                fieldName = fieldName.Replace("address_", "");
                                break;
                        }
                    }

                    entity.Attributes.Add(fieldName, data);
                }
            }

            Create(reader.GetField<decimal>("ref"), entity);

            return entity;
        }
    }
}
