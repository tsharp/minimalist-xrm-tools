using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;

namespace EpicXrm.TestFramework.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            CrmServiceClient client = new CrmServiceClient("AuthType=AD;Url=https://org/dev");

            RetrieveAllEntitiesRequest rar = new RetrieveAllEntitiesRequest()
            {
                RetrieveAsIfPublished = true,
                EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Relationships
            };

            var response = client.Execute(rar) as RetrieveAllEntitiesResponse;

            List<Sdk.XrmFakedRelationship> fakedRelationships = new List<Sdk.XrmFakedRelationship>();

            fakedRelationships.AddRange(ConvertManyToMany(response.EntityMetadata));
            fakedRelationships.AddRange(ConvertOneToMany(response.EntityMetadata));

            File.WriteAllText("relationships.json", JsonConvert.SerializeObject(fakedRelationships));

            Console.ReadKey();
        }

        public static IEnumerable<Sdk.XrmFakedRelationship> ConvertOneToMany(params EntityMetadata[] metadata)
        {
            List<Sdk.XrmFakedRelationship> fakedRelationships = new List<Sdk.XrmFakedRelationship>();

            List<string> values = new List<string>();

            foreach (var entity in metadata)
            {
                foreach (var relationship in entity.OneToManyRelationships)
                {
                    if (!values.Contains(relationship.SchemaName))
                    {
                        Sdk.XrmFakedRelationship faked = new Sdk.XrmFakedRelationship()
                        {
                            Entity1Attribute = relationship.ReferencedAttribute,
                            Entity1LogicalName = relationship.ReferencedEntity,
                            Entity2Attribute = relationship.ReferencingAttribute,
                            Entity2LogicalName = relationship.ReferencingEntity,
                            SchemaName = relationship.SchemaName,
                            RelationshipType = Sdk.XrmFakedRelationship.enmFakeRelationshipType.OneToMany
                        };
                        
                        fakedRelationships.Add(faked);

                        values.Add(relationship.SchemaName);
                        Console.WriteLine(relationship.SchemaName);
                    }
                    else
                    {
                        Console.WriteLine($"Duplicate: {relationship.SchemaName}");
                    }
                }
            }

            return fakedRelationships;
        }

        public static IEnumerable<Sdk.XrmFakedRelationship> ConvertManyToMany(params EntityMetadata[] metadata)
        {
            List<Sdk.XrmFakedRelationship> fakedRelationships = new List<Sdk.XrmFakedRelationship>();

            List<string> values = new List<string>();

            foreach (var entity in metadata)
            {
                foreach (var relationship in entity.ManyToManyRelationships)
                {
                    if (!values.Contains(relationship.SchemaName))
                    {
                        Sdk.XrmFakedRelationship faked = new Sdk.XrmFakedRelationship()
                        {
                            Entity1Attribute = relationship.Entity1IntersectAttribute,
                            Entity1LogicalName = relationship.Entity1LogicalName,
                            Entity2Attribute = relationship.Entity2IntersectAttribute,
                            Entity2LogicalName = relationship.Entity2LogicalName,
                            SchemaName = relationship.SchemaName,
                            RelationshipType = Sdk.XrmFakedRelationship.enmFakeRelationshipType.ManyToMany
                        };

                        fakedRelationships.Add(faked);

                        values.Add(relationship.SchemaName);
                        Console.WriteLine(relationship.SchemaName);
                    }
                    else
                    {
                        Console.WriteLine($"Duplicate: {relationship.SchemaName}");
                    }
                }
            }

            return fakedRelationships;
        }
    }
}
