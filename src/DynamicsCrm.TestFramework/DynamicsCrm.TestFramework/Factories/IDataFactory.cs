using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using DynamicsCrm.TestFramework.Logging;

namespace DynamicsCrm.TestFramework.Factories
{
    public class FactoryEventArgs
    {
        public string[] Data { get; set; }
        public Entity Entity { get; set; }
    }

    public delegate void ChangedEventHandeler(object sender, FactoryEventArgs e);

    public abstract class IDataFactory : IDisposable
    {
        internal ILog log = LogProvider.GetCurrentClassLogger();
        protected IOrganizationService service;

        public event ChangedEventHandeler RowProcessing;
        public event ChangedEventHandeler RowProcessed;

        protected Dictionary<string, EntityReference> Currencies = new Dictionary<string, EntityReference>();
        private Dictionary<decimal, EntityReference> createdEntities = new Dictionary<decimal, EntityReference>();

        public IDataFactory(IOrganizationService service)
        {
            this.service = service;

            var tCurrencies = service.RetrieveMultiple(new QueryExpression("transactioncurrency") { ColumnSet = new ColumnSet("isocurrencycode") }).Entities;

            foreach(var currency in tCurrencies)
            {
                Currencies.Add((string)currency["isocurrencycode"], currency.ToEntityReference());
            }
        }

        public Guid GetCurrency(string code)
        {
            return Currencies[code].Id;
        }

        protected void OnRowProcessing(params string[] data)
        {
            RowProcessing?.Invoke(this, new FactoryEventArgs() { Data = data });
        }

        protected void OnRowProcessed(Entity entity)
        {
            RowProcessed?.Invoke(this, new FactoryEventArgs() { Entity = entity });
        }

        protected abstract Entity ProcessRowData(Dictionary<string, Tuple<string, string>> dataTypes, CsvHelper.CsvReader reader);

        public void ProcessRow(Dictionary<string, Tuple<string, string>> dataTypes, CsvHelper.CsvReader reader) {
            OnRowProcessing(reader.CurrentRecord);
            var entity = ProcessRowData(dataTypes, reader);
            OnRowProcessed(entity);
        }

        protected void Create(decimal dataRef, Entity e)
        {
            var id = service.Create(e);
            createdEntities.Add(dataRef, new EntityReference(e.LogicalName, id));
        }

        public EntityReference GetRef(decimal dataRef)
        {
            return createdEntities[dataRef];
        }

        public IEnumerable<EntityReference> GetRefs(string logicalName)
        {
            return createdEntities.Where(e => e.Value.LogicalName == logicalName).Select(e => e.Value);
        }

        public void Dispose()
        {
            log.Info("Factory Cleanup ... ");

            List<EntityReference> failedCleanups = new List<EntityReference>();

            // . records indicate a sub-record which will be cleaned up by a primary record
            foreach (var record in createdEntities.OrderBy(r => r.Key).Reverse())
            {
                if((record.Key % 1) != 0)
                {
                    //log.Info($"Skipping Child Record ... {record.Value.LogicalName}: {record.Value.Id}");
                    continue;
                }

                try {
                    //Console.Write("Deleting {0}: {1} ... ", record.Value.LogicalName, record.Value.Id);
                    service.Delete(record.Value.LogicalName, record.Value.Id); }
                catch {
                    //Console.WriteLine(" Failed.");
                    failedCleanups.Add(record.Value);
                    continue;
                }

               // Log.Information("Completed.");
            }

            foreach(var record in failedCleanups)
            {
                try
                {
                    //Console.Write("Deleting {0}: {1} ... ", record.LogicalName, record.Id);
                    service.Delete(record.LogicalName, record.Id);
                }
                catch
                {
                    //Log.Information(" Failed.");
                    continue;
                }

                //Log.Information("Completed.");
            }

            log.Info("Factory Cleanup ...  Done.");
        }
    }
}
