namespace EpicXrm.WebResourceTool
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using McTools.Xrm.Connection.WinForms;
    using Mono.Options;
    using Serilog;

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            Log.Logger = log;

            bool show_help = false;
            bool publish = false;
            bool create = false;
            bool deploy = false;
            // bool pull = false;
            bool force = false;
            bool pause = false;
            bool link = false;
            bool noprompt = false;
            bool clean = false;
            bool onlyJs = false;
            bool manage_connections = false;

            OptionSet p = new OptionSet()
              {
                {
                  "publish",
                  "When used with -d or --deploy, it will publish deployed resources.",
                  (v => publish = v != null)
                },
                //{
                //    "pull",
                //    "This will pull the web-resources from CRM or from a target solution when triggered",
                //    (v => pull = v != null)
                //},
                {
                  "deploy",
                  "Deploy Package.",
                  (v => deploy = v != null)
                },
                {
                  "link",
                  "Links existing records within CRM to current package, also presents the option to link to a particular solution.",
                  (v => link = v != null)
                },
                {
                  "noprompt",
                  "Does not prompt while linking packages.",
                  (v => noprompt = v != null)
                },
                {
                  "force",
                  "Forces a complete redeployment and or publish of entire package.",
                  (v => force = v != null)
                },
                {
                  "create",
                  "Create Package and Exit. Use this to start a new solution.",
                  (v => create = v != null)
                },
                {
                  "clean",
                  "Deletes all old files from CRM system, or attempts to, and removes references once deleted.",
                  (v => clean = v != null)
                },
                {
                  "js",
                  "Only Deploys JavaScript & Mapping Files",
                  (v => onlyJs = v != null)
                },
                {
                  "pause",
                  "Pauses output when finished.",
                  (v => pause = v != null)
                },
                {
                    "conn",
                    "Manages Connections.",
                    (v=> manage_connections = v != null)
                },
                {
                  "h|help",
                  "Show this message and exit.",
                  (v => show_help = v != null)
                }
              };

            List<string> stringList = null;

            try
            {
                stringList = p.Parse(args);

                if (show_help)
                {
                    ShowHelp(p);
                }
                else if (manage_connections)
                {
                    ManageConnections();
                }
                else if (stringList == null || stringList.Count == 0)
                {
                    ShowError("You must specify a source directory.");
                }
                else if (stringList.Count > 1)
                {
                    ShowError("You cannot specify more than one source directory.");
                }
                else
                {
                    string source = stringList[0];
                    if (clean)
                    {
                        Clean(source);
                    }

                    if (create)
                    {
                        Create(source);
                        if (link)
                        {
                            Link(source, noprompt);
                        }
                    }
                    else
                    {
                        List<string> explicitExtensions = new List<string>();

                        if (onlyJs)
                        {
                            explicitExtensions.Add("js");
                            explicitExtensions.Add("map");
                        }

                        if (link)
                        {
                            Link(source, noprompt);
                        }

                        if (deploy)
                        {
                            Deploy(source, publish, force, explicitExtensions);
                        }
                    }
                }
            }
            catch (OptionException ex)
            {
                ShowError(ex.Message);
            }
            
            if (pause)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ManageConnections()
        {
            (new ConnectionSelector(false, false) { StartPosition = FormStartPosition.CenterParent }).ShowDialog();
        }
        
        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: CrmIdeTools [OPTIONS]+ C:\\SourceDirectory");
            Console.WriteLine("Builds, updates and packages CRM solutions with a given source directory.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        public static void ShowError(string message)
        {
            Console.Error.WriteLine("Web Resource Tool: ");
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("Try `wrt --help' for more information.");
        }

        private static void Deploy(string source, bool publish, bool force, List<string> explicitExtensions)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
            {
                deploymentManager.Deploy(publish, force, explicitExtensions);
            }
        }

        private static void Create(string source)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
            {
                deploymentManager.Create();
            }
        }

        private static void Link(string source, bool noprompt)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
            {
                deploymentManager.Link(noprompt);
            }
        }

        private static void Clean(string source)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
            {
                deploymentManager.clean();
            }
        }
    }
}
