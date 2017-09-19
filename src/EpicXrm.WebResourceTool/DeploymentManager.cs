﻿namespace EpicXrm.WebResourceTool
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.ServiceModel;
    using EpicXrm.WebResourceTool.Solutions;
    using McTools.Xrm.Connection;
    using System.Diagnostics;
    using McTools.Xrm.Connection.WinForms;
    using Microsoft.Xrm.Tooling.Connector;

    public class DeploymentManager : IDisposable
    {
        public const string PACKAGE_NAME = "package.crmpkg";
        private string source;
        private Package solution;
        private CrmServiceClient connection;

        public DeploymentManager(string source)
        {
            if (!Directory.Exists(source))
                Program.ShowError("Source directory does not exist!");
            else
                this.source = Path.GetDirectoryName((int)source[source.Length - 1] == 47 ? source : source + "/");
        }

        private int GetTypeFromExtension(string ext)
        {
            Console.WriteLine("Debug - Etension: " + ext);
            switch (ext.Replace(".", ""))
            {
                case "htm":
                case "html":
                    return 1;
                case "css":
                    return 2;
                case "json":
                case "js":
                case "map":
                    return 3;
                case "xml":
                    return 4;
                case "png":
                case "eot":
                case "svg":
                case "ttf":
                case "woff":
                    return 5;
                case "jpg":
                case "jpeg":
                    return 6;
                case "gif":
                    return 7;
                case "xap":
                    return 8;
                case "xsl":
                    return 9;
                case "ico":
                    return 10;
                default:
                    return -1;
            }
        }

        private DirectoryNode LoadDirectory()
        {
            DirectoryInfo di = new DirectoryInfo(this.source);
            DirectoryNode parentNode = new DirectoryNode();
            CreateFolderStructure(parentNode, di, this.source);
            return parentNode;
        }

        private void CreateFolderStructure(DirectoryNode parentNode, DirectoryInfo di, string sourceDirectory)
        {
            DirectoryNode parentNode1 = new DirectoryNode()
            {
                Name = parentNode.HasParent ? di.Name : ""
            };
            parentNode1.SetParent(parentNode);
            foreach (DirectoryInfo directory in di.GetDirectories())
                this.CreateFolderStructure(parentNode1, directory, this.source);
            foreach (FileInfo file in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                if (this.GetTypeFromExtension(file.Extension) != -1)
                {
                    WebResource webResource = new WebResource()
                    {
                        Name = parentNode1.Combine(file.Name),
                        DisplayName = file.Name,
                        WebResourceType = GetTypeFromExtension(file.Extension)
                    };
                    
                    parentNode1.Resources.Add(webResource);
                }
            }
            parentNode.Directories.Add(parentNode1);
        }

        private string GetEncodedFileContents(string pathToFile)
        {
            string str = ((IEnumerable<string>)pathToFile.Split('.')).Last();
            using (FileStream fileStream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                byte[] numArray = new byte[fileStream.Length];
                long num = fileStream.Read(numArray, 0, (int)fileStream.Length);
                return Convert.ToBase64String(numArray, 0, numArray.Length);
            }
        }

        public void Load()
        {
            Console.Write("Loading Package & Detecting File Changes... ");
            solution = Package.Load(Path.Combine(source, "package.crmpkg"));
            Console.Write("Done.\n");
            Console.Write("Detecting File & Directory Additions... ");
            solution.Files.Combine(LoadDirectory());
            Console.Write("Done.\n");
            Console.Write("Saving Package Changes... ");
            solution.Save();
            Console.Write("Done.\n");

            if (solution.SolutionId.HasValue)
            {
                Console.WriteLine();
                Console.WriteLine("Package linked to solution: " + solution.SolutionName);
            }
        }

        public void Deploy(bool publish, bool force, List<string> explicitExtensions)
        {
            if (File.Exists(Path.Combine(this.source, "package.crmpkg")))
            {
                Load();
                string currentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(this.source);
                List<Guid> source = Deploy(solution.Files, force, explicitExtensions);
                Directory.SetCurrentDirectory(currentDirectory);
                Console.Write("Saving Post Deploy Package Changes... ");
                solution.Save();
                Console.Write("Done.\n");
                if (!publish || source == null || source.Count() <= 0 || !Connect())
                {
                    return;
                }

                try
                {
                    Console.Write(string.Format("Publishing changes, {0} to update... ", source.Count()));

                    var sources = string.Join(Environment.NewLine, source.Select(s => $"<webresource>{s.ToString("B")}</webresource>"));

                    PublishXmlRequest publishXmlRequest = new PublishXmlRequest()
                    {
                        ParameterXml = $"<importexportxml><webresources>{sources}</webresources></importexportxml>"
                    };

                    if (connection.IsReady)
                    {
                        PublishXmlResponse publishXmlResponse = (PublishXmlResponse)connection.Execute(publishXmlRequest);
                        Console.Write("Success.\n");
                    }
                    else
                    {
                        Console.WriteLine(connection.LastCrmError);
                    }

                }
                catch (Exception ex)
                {
                    Console.Write("Failed.\n");
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Package does not exist. Create a deployment package before continuing.");
            }
        }

        private bool Connect()
        {
            try
            {
                ConnectionManager.Instance.LoadConnectionsList();

                ConnectionDetail connectionDetail1 = ConnectionManager.Instance.ConnectionsList.Connections.Where(conn => conn.ConnectionId.HasValue && conn.ConnectionId.Value == this.solution.ConnectionId).FirstOrDefault<ConnectionDetail>();
                if (connectionDetail1 != null)
                {
                    connection = connectionDetail1.GetCrmServiceClient();

                    if (!connection.IsReady)
                    {
                        throw new Exception(connection.LastCrmError);
                    }
                }
                else
                {

                    var wrapper = new WindowWrapper(Process.GetCurrentProcess().MainWindowHandle);

                    var selector = new ConnectionSelector(false, true)
                    {
                        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    };

                    selector.ShowDialog(System.Windows.Forms.Control.FromHandle(wrapper.Handle) as System.Windows.Forms.Form);

                    ConnectionDetail connectionDetail = selector.SelectedConnections.FirstOrDefault();
                    if (connectionDetail != null)
                    {
                        connection = connectionDetail.GetCrmServiceClient();
                        if (!connection.IsReady)
                        {
                            throw new Exception(this.connection.LastCrmError);
                        }

                        solution.ConnectionId = connectionDetail.ConnectionId.Value;
                        Console.WriteLine("Saving Connection Info");
                        solution.Save();
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect to CRM - Aborting Operations. Update connections with arg '-conn'");
                        connection = null;
                        return false;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to connect to CRM - aborting operations. Update connections with arg '-conn'");
                connection = null;
                return false;
            }

            return true;
        }

        private List<Guid> Deploy(DirectoryNode node, bool force, List<string> explicitExtnesions)
        {
            List<Guid> guidList1 = new List<Guid>();
            foreach (WebResource resource in node.Resources)
            {
                string str = ((IEnumerable<string>)resource.FileName.Split('.')).Last<string>();
                if (str == null || explicitExtnesions.Count <= 0 || explicitExtnesions.Contains(str))
                {
                    if ((!resource.Checksum.Equals(resource.GetCurrentChecksum()) || force) && !resource.Delete && !resource.Name.ToLower().Contains(".ignore"))
                    {
                        if (!Connect())
                        {
                            return null;
                        }

                        guidList1.Add(Deploy(resource, force));
                    }

                    else if (resource.Name.ToLower().Contains(".ignore"))
                    {
                        Console.WriteLine(resource.Name + " Ignored.");

                        if (resource.WebResourceId == Guid.Empty)
                        {
                            resource.WebResourceId = Guid.NewGuid();
                        }

                        resource.UpdateChecksum();
                    }
                }
            }
            foreach (DirectoryNode directory in node.Directories)
            {
                List<Guid> guidList2 = Deploy(directory, force, explicitExtnesions);
                if (guidList2 != null)
                {
                    guidList1.AddRange(guidList2);
                }
            }
            return guidList1;
        }

        private Guid Deploy(WebResource resource, bool force)
        {
            try
            {
                if (this.connection.IsReady)
                {
                    Entity entity = new Entity("webresource");
                    entity["name"] = RemoveResourceExtension(resource.Name, resource.RemoveExtension);
                    entity["displayname"] = resource.DisplayName;
                    entity["ishidden"] = resource.IsHidden;
                    entity["canbedeleted"] = resource.CanBeDeleted;

                    if (((IEnumerable<string>)resource.Name.Split('.')).Last().Equals("xap"))
                    {
                        entity["silverlightversion"] = "5";
                    }

                    entity["description"] = resource.Description;
                    entity["webresourcetype"] = new OptionSetValue(resource.WebResourceType);
                    entity["content"] = GetEncodedFileContents(resource.Name);

                    if (resource.WebResourceId != Guid.Empty)
                    {
                        if (this.connection.Retrieve("webresource", resource.WebResourceId, new ColumnSet(new string[0])) != null)
                        {
                            Console.Write("Updating Resource: " + resource.Name + "... ");
                            entity.Id = resource.WebResourceId;
                            this.connection.Update(entity);
                        }
                    }
                    else
                    {
                        Console.Write("Creating Resource: " + resource.Name + "... ");
                        resource.WebResourceId = this.connection.Create(entity);
                    }
                    Console.Write("Success.\n");
                    resource.UpdateChecksum();

                    if (this.solution.SolutionId.HasValue)
                    {
                        try
                        {
                            Console.Write("Linking Entity To Solution... ");
                            AddSolutionComponentRequest componentRequest = new AddSolutionComponentRequest()
                            {
                                ComponentType = 61,
                                SolutionUniqueName = this.solution.SolutionUniqueName,
                                ComponentId = resource.WebResourceId
                            };
                            this.connection.Execute((OrganizationRequest)componentRequest);
                            Console.Write("Done.\n");
                        }
                        catch (Exception ex)
                        {
                            Console.Write("Failed.\n");
                            Console.WriteLine(ex.Message);
                        }
                    }
                    return resource.WebResourceId;
                }
                else
                    throw new Exception(this.connection.LastCrmError);
            }
            catch (Exception ex)
            {
                Console.Write("Failed.\nException: " + ex.Message + "\n");
                if (ex.Message.ToLower().Contains("does not exist"))
                {
                    Console.WriteLine("Cleaning up dangling Web Resource Id... Done.");
                    Console.WriteLine("File Will Import Cleanly During Next Deploy.");
                    resource.WebResourceId = Guid.Empty;
                }
                return Guid.Empty;
            }
        }

        public bool Create()
        {
            try
            {
                var packagePath = Path.Combine(source, "package.crmpkg");

                if (!File.Exists(packagePath))
                {
                    Console.Write("Creating Package... ");
                    solution = new Package()
                    {
                        Files = LoadDirectory(),
                        PackageFileName = packagePath
                    };

                    solution.Files.Initialize();

                    solution.Save();

                    Console.Write("Done.");
                }
                else
                {
                    Console.WriteLine("Package already exists.");
                }
            }
            catch (Exception ex)
            {
                Console.Write("Failed.");
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public bool Link(bool noprompt)
        {
            try
            {
                Load();
                if (!noprompt)
                {
                    Console.Write("\nDo you want to link the current package to an existing solution? y/n: ");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        Console.WriteLine();
                        LinkSolution();
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                Console.WriteLine("Linking Entitiy Records... ");
                Link(solution.Files);
                Console.WriteLine("Linking Completed.");

                Console.Write("Saving Entity Link Package Changes... ");
                solution.Save();
                Console.Write("Done.\n");
            }
            catch (Exception ex)
            {
                Console.Write("Failed.\n");
                Console.WriteLine(ex.Message);
            }

            return true;
        }

        private void Link(DirectoryNode node)
        {
            if (!Connect())
            {
                return;
            }

            foreach (WebResource resource in node.Resources)
            {
                Link(resource);
            }

            foreach (DirectoryNode directory in node.Directories)
            {
                Link(directory);
            }
        }

        private void Link(WebResource resource)
        {
            try
            {
                if (this.connection.IsReady)
                {
                    Console.Write("Checking for link: " + resource.Name + "... ");
                    QueryExpression linkFindQuery1 = CreateLinkFindQuery(resource, false);
                    EntityCollection entityCollection1 = connection.RetrieveMultiple(linkFindQuery1);

                    if (entityCollection1.Entities.Count() == 1)
                    {
                        resource.WebResourceId = entityCollection1[0].Id;
                        Console.Write("Found.\n");
                    }
                    else if (entityCollection1.Entities.Count() > 1)
                    {
                        Console.Write("WAO. You have multiple web resources in your system with the same name. Please fix this immedately before continuing.\n");
                    }
                    else
                    {
                        QueryExpression linkFindQuery2 = CreateLinkFindQuery(resource, true);
                        EntityCollection entityCollection2 = connection.RetrieveMultiple(linkFindQuery2);
                        if (entityCollection2.Entities.Count() == 1)
                        {
                            resource.WebResourceId = entityCollection2[0].Id;
                            resource.RemoveExtension = true;
                            Console.Write("Found.\n");
                        }
                        else
                        {
                            Console.Write("None.\n");
                        }
                    }
                }
                else
                {
                    throw new Exception(connection.LastCrmError);
                }
            }
            catch (Exception ex)
            {
                Console.Write("Failed.\n");
                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        private void SyncFromServer()
        {
            if (!Connect())
            {
                return;
            }
        }

        private void LinkSolution()
        {
            if (!Connect())
            {
                return;
            }

            EntityCollection entityCollection = RetrieveAllUnmanagedSolutions(null);
            SortedList<int, Guid> sortedList1 = new SortedList<int, Guid>();
            SortedList<Guid, string> source = new SortedList<Guid, string>();
            SortedList<Guid, string> sortedList2 = new SortedList<Guid, string>();

            foreach (Entity entity in entityCollection.Entities)
            {
                sortedList2.Add(entity.Id, (string)entity["uniquename"]);
            }

            Console.WriteLine("Available Solutions:");
            int result;

            while (true)
            {
                sortedList1.Clear();
                source.Clear();
                foreach (Entity entity in entityCollection.Entities)
                {
                    string str = entity["friendlyname"].ToString() + " - " + ((EntityReference)entity["publisherid"]).Name;
                    source.Add(entity.Id, str);
                    sortedList1.Add(source.Count(), entity.Id);
                    Console.WriteLine(source.Count().ToString() + ") " + str);
                }
                result = -1;
                Console.WriteLine("Enter x to exit or choose a number. Numbers are next to solutions.");
                string s = Console.ReadLine();
                if (s.Length != 1 || !s.Equals("x"))
                {
                    if (!int.TryParse(s, out result) || !sortedList1.ContainsKey(result))
                    {
                        Console.WriteLine("Invalid Choice, press any key.");
                        Console.ReadKey();
                    }
                    else
                        goto label_19;
                }
                else
                    break;
            }
            Console.WriteLine("Operation Canceled.");
            return;
            label_19:
            Console.WriteLine("Linking To: " + source[sortedList1[result]]);
            solution.SolutionName = source[sortedList1[result]];
            solution.SolutionId = new Guid?(sortedList1[result]);
            solution.SolutionUniqueName = sortedList2[sortedList1[result]];
        }

        public void clean()
        {
            try
            {
                Load();
                Clean(solution.Files);
                Console.Write("Saving Package Changes... ");
                solution.Save();
                Console.Write("Done.\n");
            }
            catch (Exception ex)
            {
                Console.Write("Failed.\n");
                Console.WriteLine(ex.Message);
            }
        }

        private void Clean(DirectoryNode node)
        {
            List<WebResource> webResourceList = new List<WebResource>();
            foreach (WebResource resource in node.Resources)
            {
                if (!this.Connect())
                    return;
                int num;
                if (resource.Delete || resource.Name.ToLower().Contains(".ignore"))
                {
                    if (this.GetTypeFromExtension(((IEnumerable<string>)resource.Name.Split('.')).Last<string>()) != -1)
                    {
                        num = !this.Clean(resource) ? 1 : 0;
                        goto label_7;
                    }
                }
                num = 1;
                label_7:
                if (num == 0 && !resource.Name.ToLower().Contains(".ignore"))
                    webResourceList.Add(resource);
            }
            foreach (WebResource webResource in webResourceList)
                node.Resources.Remove(webResource);
            foreach (DirectoryNode directory in node.Directories)
                this.Clean(directory);
        }

        private bool Clean(WebResource resource)
        {
            try
            {
                if (this.connection.IsReady)
                {
                    Console.Write("Deleting " + resource.Name + "... ");
                    this.connection.Delete("webresource", resource.WebResourceId);
                    Console.Write("Success.\n");
                    return true;
                }
                else
                    throw new Exception(this.connection.LastCrmError);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                if (ex.Message.ToLower().Contains("does not exist"))
                {
                    Console.Write("Success.\n");
                    Console.WriteLine("Warning: File did not exist in CRM System.");
                    return true;
                }
                Console.Write("Failed.\n");
                Console.WriteLine("Exception: " + ex.Message);
                return false;
            }
        }

        private EntityCollection RetrieveAllUnmanagedSolutions(List<Guid> optionalId = null)
        {
            QueryExpression queryExpression = new QueryExpression("solution");
            queryExpression.ColumnSet = new ColumnSet(true);
            queryExpression.Criteria = new FilterExpression(LogicalOperator.And);
            queryExpression.Criteria.AddCondition(new ConditionExpression("ismanaged", ConditionOperator.Equal, (object)false));
            queryExpression.Criteria.AddCondition(new ConditionExpression("isvisible", ConditionOperator.Equal, (object)true));
            queryExpression.Criteria.AddCondition(new ConditionExpression("uniquename", ConditionOperator.NotEqual, (object)"Default"));
            if (optionalId != null && optionalId.Count > 0)
                queryExpression.Criteria.AddCondition(new ConditionExpression("solutionid", ConditionOperator.In, (ICollection)optionalId.ToArray()));

            if (!this.connection.IsReady)
                throw new Exception(this.connection.LastCrmError);

            return this.connection.RetrieveMultiple((QueryBase)queryExpression);
        }

        private QueryExpression createSolutionQuery()
        {
            return new QueryExpression("solution");
        }

        private QueryExpression CreateLinkFindQuery(WebResource resource, bool removeExtension = false)
        {
            if (removeExtension)
                Console.Write(resource.Name.Replace(Path.GetExtension(resource.Name), " "));
            QueryExpression queryExpression = new QueryExpression("webresource");
            queryExpression.Criteria = new FilterExpression();
            queryExpression.Criteria.AddCondition("name", ConditionOperator.Equal, (object)this.RemoveResourceExtension(resource.Name, removeExtension || resource.RemoveExtension));
            queryExpression.ColumnSet = new ColumnSet(new string[0]);
            return queryExpression;
        }

        public void Dispose()
        {
        }

        private string RemoveResourceExtension(string name, bool remove)
        {
            return remove ? name.Replace(Path.GetExtension(name), "") : name;
        }
    }
}
