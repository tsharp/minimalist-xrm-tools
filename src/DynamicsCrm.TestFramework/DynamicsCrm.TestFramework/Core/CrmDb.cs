namespace DynamicsCrm.TestFramework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using DynamicsCrm.TestFramework.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;

    [DataContract(Namespace = "http://www.orbitalforge.com/DynamicsCrm/UnitTest")]

    public class CrmDb : List<Entity>
    {
        ILog log = LogProvider.For<CrmDb>();

        private bool DbContainsId(string logicalName, Guid id)
        {
            return this.Where(e => e.LogicalName == logicalName && e.Id == id).Any();
        }

        private void Normalize(Entity record)
        {
            record.LogicalName = record.LogicalName.ToLower();
        }

        private Guid GetUUID(string logicalName)
        {
            var id = Guid.NewGuid();
            while (DbContainsId(logicalName, id)) id = Guid.NewGuid();
            return id;
        }

        public new Guid Add(Entity entity)
        {
            Normalize(entity);
            if (entity.Id == Guid.Empty) entity.Id = GetUUID(entity.LogicalName);
            Assert.IsFalse(DbContainsId(entity.LogicalName, entity.Id));

            // Set Id Field - WARNING - This is different for activities.
            entity[entity.LogicalName + "id"] = entity.Id;

            // Setup Default State And Status
            if (!entity.Contains("statecode")) entity["statecode"] = new OptionSetValue(0);
            if (!entity.Contains("statuscode")) entity["statuscode"] = new OptionSetValue(1);
            if (!entity.Contains("createdon")) entity["createdon"] = DateTime.UtcNow;
            entity["modifiedon"] = DateTime.UtcNow;

            switch (entity.LogicalName)
            {
                case "quote":
                    entity["quotenumber"] = string.Format("QUO-{0}", Guid.NewGuid().ToString());
                    break;
                case "salesorder":
                    entity["ordernumber"] = string.Format("SO-{0}", Guid.NewGuid().ToString());
                    break;
                case "invoice":
                    entity["invoicenumber"] = string.Format("INV-{0}", Guid.NewGuid().ToString());
                    break;
            }

            log.Debug($"Creating {entity.LogicalName}:{entity.Id}");

            base.Add(entity);
            return entity.Id;
        }

        public void Remove(string logicalName, Guid id)
        {
            this.Remove(Get(logicalName, id));
        }

        /// <summary>
        /// TODO: Trim columns based on column set to represent an actual retrieve.
        /// </summary>
        /// <param name="logicalName"></param>
        /// <param name="id"></param>
        public Entity Get(string logicalName, Guid id)
        {
            Assert.IsTrue(DbContainsId(logicalName.ToLower(), id));
            return this.Where(e => e.LogicalName == logicalName.ToLower() && e.Id == id).First();
        }

        public void Update(Entity entity)
        {
            var existing = Get(entity.LogicalName, entity.Id);

            foreach (var key in entity.Attributes.Keys)
            {
                existing[key] = entity[key];
            }
        }

        private static IEnumerable<Entity> Criteria_EqualCheck(IEnumerable<Entity> records, string attribName, DataCollection<object> values, bool equalIsSuccess)
        {
            List<Entity> results = new List<Entity>();
            var compareValue = values.FirstOrDefault();

            var pFe = Parallel.ForEach(records, (Entity record) =>
            {
                object value = UnwrapValue(record, attribName);
                if (value == null) return;

                if (equalIsSuccess)
                {
                    if ((value is EntityReference && (value as EntityReference).Id.Equals(compareValue)) ||
                    (value is Guid && value.Equals(compareValue.ToString())) ||
                    value.Equals(compareValue))
                    {
                        results.Add(record);
                    }
                }
                else
                {
                    if (
                        !(
                            (value is EntityReference && (value as EntityReference).Id.Equals(compareValue)) ||
                            (value is Guid && value.Equals(compareValue.ToString())) ||
                            value.Equals(compareValue)
                        )
                       )
                    {
                        results.Add(record);
                    }
                }
            });

            while (!pFe.IsCompleted)
            {
                Thread.Sleep(25);
            }

            return results;
        }

        private static object UnwrapValue(Entity record, string attribute)
        {
            if (!record.Contains(attribute))
            {
                return null;
            }

            object value = null;

            if (record[attribute] is Guid)
            {
                value = record[attribute].ToString();
            }

            if (record[attribute] is OptionSetValue)
            {
                value = (record[attribute] as OptionSetValue).Value;
            }

            if (record[attribute] is Money)
            {
                value = (record[attribute] as Money).Value;
            }

            return value;
        }

        private static IEnumerable<Entity> Criteria_In(IEnumerable<Entity> records, string attribName, DataCollection<object> values)
        {
            List<Entity> results = new List<Entity>();
            var pFe = Parallel.ForEach(records.Where(r => r.Contains(attribName) && r[attribName] != null), (Entity record) =>
            {
                object value = UnwrapValue(record, attribName);

                if (values.Any(v => v.Equals(value)))
                {
                    results.Add(record);
                }
            });

            while (!pFe.IsCompleted)
            {
                Thread.Sleep(25);
            }

            return results;
        }

        private static IEnumerable<Entity> Criteria_NullCheck(IEnumerable<Entity> records, string attribName, bool nullIsSuccess)
        {
            List<Entity> results = new List<Entity>();
            var pFe = Parallel.ForEach(records.Where(r => r.Contains(attribName) && r[attribName] != null), (Entity record) =>
            {
                object value = UnwrapValue(record, attribName);

                if ((value == null && nullIsSuccess) ||
                    (value != null && !nullIsSuccess))
                {
                    results.Add(record);
                }
            });

            while (!pFe.IsCompleted)
            {
                Thread.Sleep(25);
            }

            return results;
        }

        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            EntityCollection collection = new EntityCollection();
            collection.PagingCookie = "WACK";
            collection.EntityName = query.EntityName.ToLower();

            var records = this.Where(e => e.LogicalName == collection.EntityName);

            #region Criterion Filters
            if (query.Criteria != null)
            {
                foreach (ConditionExpression criterion in query.Criteria.Conditions)
                {
                    switch (criterion.Operator)
                    {
                        case ConditionOperator.Equal:
                            records = Criteria_EqualCheck(records, criterion.AttributeName, criterion.Values, true);
                            break;
                        case ConditionOperator.NotNull:
                            records = Criteria_NullCheck(records, criterion.AttributeName, false);
                            break;
                        case ConditionOperator.Null:
                            records = Criteria_NullCheck(records, criterion.AttributeName, true);
                            break;
                        case ConditionOperator.NotEqual:
                            records = Criteria_EqualCheck(records, criterion.AttributeName, criterion.Values, false);
                            break;
                        case ConditionOperator.In:
                            records = Criteria_In(records, criterion.AttributeName, criterion.Values);
                            break;
                        default:
                            throw new NotImplementedException("Criterion Filter Not Implemented: " + criterion.Operator);
                    }
                }
            }
            #endregion

            #region Link Entity Filters
            // TODO: Implement Joins
            #endregion

            if (records.Count() > 0) collection.Entities.AddRange(records);

            return collection;
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            if (query.GetType() == typeof(QueryExpression)) return RetrieveMultiple((QueryExpression)query);
            throw new NotImplementedException("Unknown Query Type: " + query.GetType().FullName);
        }

        public void SetState(string logicalName, Guid id, int state, int status)
        {
            if (state < 0) throw new Exception("Default State Codes Not Supported");
            if (status < 0) throw new Exception("Default Status Codes Not Supported");

            Entity e = new Entity(logicalName) { Id = id };
            e["statecode"] = new OptionSetValue(state);
            e["statuscode"] = new OptionSetValue(status);

            Update(e);
        }
    }
}
