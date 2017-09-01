using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

internal static class ServiceHelpers
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

    public static byte[] ExportSolution(this IOrganizationService service, string solutionName, bool isManaged)
    {
        Console.WriteLine("Exporting {0} as Managed = {1} ...", solutionName, isManaged);
        return ((ExportSolutionResponse)service.Execute(new ExportSolutionRequest()
        {
            SolutionName = solutionName,
            Managed = isManaged
        })).ExportSolutionFile;
    }

    public static Dictionary<string, byte[]> ExportSolutions(this IOrganizationService service, string solutionName, bool isManaged)
    {
        return null;
    }

    public static Dictionary<string, byte[]> ExportProductSolutions(this IOrganizationService service, string productName, bool isManaged)
    {
        Dictionary<string, byte[]> exports = new Dictionary<string, byte[]>();

        var solutions = service.GetProductSolutions(productName, false);

        foreach (var solution in solutions)
        {
            exports.Add(solution["uniquename"].ToString(), service.ExportSolution(solution["uniquename"].ToString(), isManaged));
        }

        return exports;
    }

    public static void UpdateProductVersion(this IOrganizationService service, string productName, string versionNumber)
    {
        var solutions = GetProductSolutions(service, productName);
        foreach (var solution in solutions)
        {
            service.UpdateField(solution.LogicalName, solution.Id, "version", versionNumber);
        }
    }

    public static Entity[] GetProductSolutions(this IOrganizationService service, string productName, bool isManaged = false)
    {
        QueryExpression query = new QueryExpression("solution");
        query.ColumnSet = new ColumnSet(true);
        // Only Include Visible Solutions
        query.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);

        // Managed Filter
        query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, isManaged);

        // Product Filter - product solutions always start with the same prefix if following the enterprise rules.
        query.Criteria.AddCondition("uniquename", ConditionOperator.BeginsWith, productName);

        // Exclude Default Solution
        query.Criteria.AddCondition("uniquename", ConditionOperator.NotEqual, "Default");
        return service.RetrieveMultiple(query).Entities.ToArray();
    }

    public static Entity GetSolution(this IOrganizationService service, string solutionName)
    {;
        QueryExpression query = new QueryExpression("solution");
        query.ColumnSet = new ColumnSet(true);

        // Product Filter - product solutions always start with the same prefix if following the enterprise rules.
        query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);

        return service.RetrieveMultiple(query).Entities.FirstOrDefault();
    }

    public static string GetSolutionVersion(this IOrganizationService service, string solutionName)
    {
        var solution = service.GetSolution(solutionName);
        if (solution == null) return "0.0.0.0";
        return solution["version"].ToString();   
    }

    public static bool UpdatePluginAssembly(this IOrganizationService service, string fileName)
    {
        var assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
        var assemblyName = assembly.GetName();
        var publicKeyToken = assembly.FullName.Split(',').Select(p => p.Trim().Split('=').Last()).Last();
        var data = File.ReadAllBytes(fileName);
        FilterExpression filter = new FilterExpression();
        filter.Conditions.Add(new ConditionExpression("publickeytoken", ConditionOperator.Equal, publicKeyToken));
        filter.Conditions.Add(new ConditionExpression("name", ConditionOperator.Equal, assemblyName.Name));
        var crmAssemblyId = service.RetrieveAllRecords("pluginassembly", new ColumnSet(true), filter).First().Id;

        var update = new Entity("pluginassembly", crmAssemblyId);
        update["content"] = Convert.ToBase64String(data);

        service.Update(update);

        return true;
    }
}

