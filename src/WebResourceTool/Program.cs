namespace Minimalist.Dynamics.WebResourceTool
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using McTools.Xrm.Connection.WinForms;
    using Mono.Options;


    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool show_help = false;
            bool publish = false;
            bool create = false;
            bool deploy = false;
            bool force = false;
            bool pause = false;
            bool link = false;
            bool noprompt = false;
            bool clean = false;
            bool min = false;
            bool onlyJs = false;
            bool manage_connections = false;
            OptionSet p = new OptionSet()
      {
        {
          "publish",
          "When used with -d or --deploy, it will publish deployed resources.",
          (Action<string>) (v => publish = v != null)
        },
        {
          "deploy",
          "Deploy Package.",
          (Action<string>) (v => deploy = v != null)
        },
        {
          "link",
          "Links existing records within CRM to current package, also presents the option to link to a particular solution.",
          (Action<string>) (v => link = v != null)
        },
        {
          "noprompt",
          "Does not prompt while linking packages.",
          (Action<string>) (v => noprompt = v != null)
        },
        {
          "force",
          "Forces a complete redeployment and or publish of entire package.",
          (Action<string>) (v => force = v != null)
        },
        {
          "create",
          "Create Package and Exit. Use this to start a new solution.",
          (Action<string>) (v => create = v != null)
        },
        {
          "clean",
          "Deletes all old files from CRM system, or attempts to, and removes references once deleted.",
          (Action<string>) (v => clean = v != null)
        },
        {
          "js",
          "Only Deploys JavaScript",
          (Action<string>) (v => onlyJs = v != null)
        },
        {
          "min",
          "Minifies CSS and JS files when imported into CRM.",
          (Action<string>) (v => min = v != null)
        },
        {
          "pause",
          "Pauses output when finished.",
          (Action<string>) (v => pause = v != null)
        },
        {
            "conn",
            "Manages Connections.",
            (Action<string>) (v=> manage_connections = v != null)
        },
        {
          "h|help",
          "Show this message and exit.",
          (Action<string>) (v => show_help = v != null)
        }
      };
            List<string> stringList;
            try
            {
                stringList = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("CrmIdeTools: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `CrmIdeTools --help' for more information.");
                if (!pause)
                    return;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }
            if (show_help)
            {
                Program.ShowHelp(p);
                if (!pause)
                    return;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            if(manage_connections)
            {

                Program.ManageConnections();
                if (!pause)
                    return;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();

            }
            else if (stringList.Count == 0)
            {
                Program.ShowError("You must specify a source directory.");
                if (!pause)
                    return;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            else if (stringList.Count > 1)
            {
                Program.ShowError("You cannot specify more than one source directory.");
                if (!pause)
                    return;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            else
            {
                string source = stringList[0];
                if (clean)
                    Program.Clean(source);
                if (create)
                {
                    Program.Create(source);
                    if (link)
                        Program.Link(source, noprompt);
                }
                else
                {
                    List<string> explicitExtensions = new List<string>();
                    if (onlyJs)
                        explicitExtensions.Add("js");
                    if (link)
                        Program.Link(source, noprompt);
                    if (deploy)
                        Program.Deploy(source, publish, min, force, explicitExtensions);
                }
                if (pause)
                {
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                }
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
            Console.Error.WriteLine("CrmIdeTools: ");
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("Try `CrmIdeTools --help' for more information.");
        }

        private static void Deploy(string source, bool publish, bool min, bool force, List<string> explicitExtensions)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
                deploymentManager.deploy(publish, min, force, explicitExtensions);
        }

        private static void Create(string source)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
                deploymentManager.create();
        }

        private static void Link(string source, bool noprompt)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
                deploymentManager.link(noprompt);
        }

        private static void Clean(string source)
        {
            using (DeploymentManager deploymentManager = new DeploymentManager(source))
                deploymentManager.clean();
        }
    }
}
