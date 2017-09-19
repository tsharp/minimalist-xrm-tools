using System;
using System.IO;
using System.Xml.Serialization;

namespace EpicXrm.WebResourceTool.Solutions
{
    [Serializable]
    public class Package
    {
        [XmlElement("Resources")]
        public DirectoryNode Files;

        public Guid? SolutionId { get; set; } = null;

        public string SolutionName { get; set; } = string.Empty;

        public string SolutionUniqueName { get; set; } = string.Empty;

        public Guid ConnectionId { get; set; }

        [XmlIgnore]
        public string PackageFileName { get; set; }
        
        public void Save()
        {
            SaveAs(PackageFileName);
        }

        public void SaveAs(string fileName)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Package));
            using (FileStream fileStream = File.Create(fileName))
            {
                xmlSerializer.Serialize(fileStream, this);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        public static Package Load(string fileName)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(fileName));
            
            Package package = null;

            using (FileStream fileStream = File.OpenRead(Path.Combine(currentDirectory, fileName)))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Package));
                package = (Package)xmlSerializer.Deserialize(fileStream);
                package.PackageFileName = fileName;
                fileStream.Close();
            }

            if (package.Files != null)
            {
                package.Files.Initialize();
            }

            Directory.SetCurrentDirectory(currentDirectory);

            return package;
        }
    }
}
