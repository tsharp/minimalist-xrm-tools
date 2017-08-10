using System;
using System.IO;
using System.Xml.Serialization;

namespace Minimalist.Dynamics.WebResourceTool.Solutions
{
    [Serializable]
    public class Package
    {
        public Guid? SolutionId = new Guid?();
        public string SolutionName = "";
        public string SolutionUniqueName = "";
        public Guid ConnectionId = Guid.Empty;
        [XmlElement("Resources")]
        public DirectoryNode Files;

        public void save(string filename)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Package));
            FileStream fileStream = File.Create(filename);
            xmlSerializer.Serialize((Stream)fileStream, (object)this);
            fileStream.Flush();
            fileStream.Close();
        }

        public static Package load(string filename)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(filename));
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Package));
            FileStream fileStream = File.OpenRead(Path.Combine(currentDirectory, filename));
            Package package = (Package)xmlSerializer.Deserialize((Stream)fileStream);
            fileStream.Close();
            if (package.Files != null)
                package.Files.initialize();
            Directory.SetCurrentDirectory(currentDirectory);
            return package;
        }
    }
}
