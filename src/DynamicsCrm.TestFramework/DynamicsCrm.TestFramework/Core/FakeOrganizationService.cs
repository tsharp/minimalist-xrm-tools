namespace DynamicsCrm.TestFramework.Core
{
    using System;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;

    public class FakeOrganizationService : IOrganizationService
    {
        private CrmDb _internalDb = new CrmDb();

        public FakeOrganizationService()
        {
            _internalDb.CreateDefaultData();
        }

        public FakeOrganizationService(CrmDb db)
        {
            _internalDb = db;
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotImplementedException();
        }

        public Guid Create(Entity entity)
        {
            return _internalDb.Add(entity);
        }

        public void Delete(string entityName, Guid id)
        {
            _internalDb.Remove(entityName, id);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotImplementedException();
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            switch (request.RequestName)
            {
                case "SetState":
                    var ssReq = (SetStateRequest)request;
                    _internalDb.SetState(ssReq.EntityMoniker.LogicalName, ssReq.EntityMoniker.Id, ssReq.State.Value, ssReq.Status.Value);
                    return new SetStateResponse();
                case "RetrieveMultiple":
                    var rmReq = (RetrieveMultipleRequest)request;

                    var resp = new RetrieveMultipleResponse();
                    resp.Results = new ParameterCollection();
                    resp.Results.Add("EntityCollection", _internalDb.RetrieveMultiple(rmReq.Query));

                    return resp;
                default:
                    throw new NotImplementedException();
            }
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return _internalDb.Get(entityName, id);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            Assert.IsInstanceOfType(query, typeof(QueryExpression), "The query is not a QueryExpression.");
            return _internalDb.RetrieveMultiple((QueryExpression)query);
        }

        public void Update(Entity entity)
        {
            _internalDb.Update(entity);
        }
    }
}
