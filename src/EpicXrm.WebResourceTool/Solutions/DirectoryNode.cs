using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace EpicXrm.WebResourceTool.Solutions
{
    [Serializable]
    public class DirectoryNode
    {
        [XmlAttribute]
        public string Name;
        public List<WebResource> Resources = new List<WebResource>();
        public List<DirectoryNode> Directories = new List<DirectoryNode>();
        private DirectoryNode parent = null;

        public bool HasParent
        {
            get
            {
                return parent != null;
            }
        }

        public void SetParent(DirectoryNode parent)
        {
            this.parent = parent;
        }

        public void Initialize()
        {
            foreach (DirectoryNode directory in this.Directories)
            {
                directory.SetParent(this);
                directory.Initialize();
            }
        }

        public string Combine(string name)
        {
            if (parent != null && Name != string.Empty)
            {
                return parent.Combine(Name) + "/" + name;
            }

            return name;
        }

        public void Combine(DirectoryNode node)
        {
            if (Resources == null)
            {
                Resources = new List<WebResource>();
            }

            if (Directories == null)
            {
                Directories = new List<DirectoryNode>();
            }

            Resources.AddRange(node.Resources.Where(resource => !ContainsResource(resource.Name)));

            foreach (DirectoryNode directory in node.Directories)
            {
                if (!ContainsDirectory(directory.Name))
                {
                    Directories.Add(directory);
                }
                else
                {
                    GetDirectory(directory.Name).Combine(directory);
                }
            }
        }

        public bool ContainsResource(string name)
        {
            return Resources.Where(resource => resource.Name.Equals(name)).Any();
        }

        public bool ContainsDirectory(string name)
        {
            return Directories.Where(directory => directory.Name.Equals(name)).Any();
        }

        public DirectoryNode GetDirectory(string name)
        {
            foreach (DirectoryNode directory in Directories)
            {
                if (directory.Name == name)
                {
                    return directory;
                }
            }

            return null;
        }
    }
}
