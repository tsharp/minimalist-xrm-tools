using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Minimalist.Dynamics.WebResourceTool.Solutions
{
    [Serializable]
    public class DirectoryNode
    {
        private DirectoryNode parent = (DirectoryNode)null;
        [XmlAttribute]
        public bool Minify = false;
        public List<WebResource> Resources = new List<WebResource>();
        public List<DirectoryNode> Directories = new List<DirectoryNode>();
        [XmlAttribute]
        public string Name;

        public bool HasParent
        {
            get
            {
                return this.parent != null;
            }
        }

        public void setParent(DirectoryNode parent)
        {
            this.parent = parent;
        }

        public void initialize()
        {
            foreach (DirectoryNode directory in this.Directories)
            {
                directory.setParent(this);
                directory.initialize();
            }
            foreach (WebResource resource in this.Resources)
                resource.validate();
        }

        public string combine(string name)
        {
            if (this.parent != null && this.Name != string.Empty)
                return this.parent.combine(this.Name) + "/" + name;
            return name;
        }

        public void combine(DirectoryNode node)
        {
            if (this.Resources == null)
                this.Resources = new List<WebResource>();
            if (this.Directories == null)
                this.Directories = new List<DirectoryNode>();
            foreach (WebResource resource in node.Resources)
            {
                if (!this.containsResource(resource.Name))
                    this.Resources.Add(resource);
            }
            foreach (DirectoryNode directory in node.Directories)
            {
                if (!this.containsDirectory(directory.Name))
                    this.Directories.Add(directory);
                else
                    this.getDirectory(directory.Name).combine(directory);
            }
        }

        public bool containsResource(string name)
        {
            foreach (WebResource resource in this.Resources)
            {
                if (resource.Name == name)
                    return true;
            }
            return false;
        }

        public bool containsDirectory(string name)
        {
            foreach (DirectoryNode directory in this.Directories)
            {
                if (directory.Name == name)
                    return true;
            }
            return false;
        }

        public DirectoryNode getDirectory(string name)
        {
            foreach (DirectoryNode directory in this.Directories)
            {
                if (directory.Name == name)
                    return directory;
            }
            return (DirectoryNode)null;
        }
    }
}
