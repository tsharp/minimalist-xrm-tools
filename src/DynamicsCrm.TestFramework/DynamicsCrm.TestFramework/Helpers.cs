using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class TestHelpers
{
    public static IEnumerable<Entity> RetrieveAllRecords(this IOrganizationService service, string logicalName, ColumnSet columns = null, FilterExpression criteria = null)
    {
        QueryExpression query = new QueryExpression(logicalName);
        query.Criteria = criteria;
        query.ColumnSet = columns;

        query.PageInfo = new PagingInfo() { Count = 500, PageNumber = 1 };

        while (true)
        {
            var result = service.RetrieveMultiple(query);
            foreach (var record in result.Entities) yield return record;

            if (result.Entities.Count < 500) break;
            query.PageInfo.PageNumber += 1;
        }

    }

    public static void DeleteAllRecords(this IOrganizationService service, string logicalName)
    {
        var records = service.RetrieveAllRecords(logicalName);
        foreach (var record in records)
        {
            service.Delete(record.LogicalName, record.Id);
        }
    }
    public static void TriggerUpdate(this IOrganizationService service, EntityReference reference)
    {
        Entity e = new Entity(reference.LogicalName) { Id = reference.Id };
        service.Update(e);
    }

    public static void SetState(this IOrganizationService service, EntityReference reference, int state, int status)
    {
        SetStateRequest request = new SetStateRequest();
        request.EntityMoniker = reference;
        request.State = new OptionSetValue(state);
        request.Status = new OptionSetValue(status);
        service.Execute(request);
    }

    public static void SetState(this IOrganizationService service, string logicalName, Guid id, int state, int status)
    {
        SetStateRequest request = new SetStateRequest();
        request.EntityMoniker = new EntityReference(logicalName, id);
        request.State = new OptionSetValue(state);
        request.Status = new OptionSetValue(status);
        service.Execute(request);
    }

    public static void UpdateField(this IOrganizationService service, string logicalName, Guid id, string field, object value)
    {
        Entity e = new Entity(logicalName) { Id = id };
        e[field] = value;
        service.Update(e);
    }
}