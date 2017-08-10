using System;
using System.IO;

namespace Minimalist.Dynamics.WebResourceTool.Solutions
{
    [Serializable]
    public class WebResource
    {
        public Guid WebResourceId = Guid.Empty;
        public bool RemoveExtension = false;
        public int LanguageCode = 1033;
        public bool IsCustomizable = true;
        public bool CanBeDeleted = true;
        public bool IsHidden = false;
        public bool Minify = false;
        public bool Delete = false;
        public DateTime LastImport = DateTime.MinValue;
        public DateTime ModifiedOn = DateTime.MinValue;
        public string Name;
        public string DisplayName;
        public string Description;
        public int WebResourceType;

        public string FileName
        {
            get
            {
                return "/WebResources/" + this.Name.Replace("//", "").Replace(".", "") + this.WebResourceId.ToString();
            }
        }

        public bool FileExists()
        {
            this.Delete = !File.Exists(this.Name);
            return File.Exists(this.Name);
        }

        public void validate()
        {
            if (!this.FileExists())
                return;
            this.ModifiedOn = File.GetLastWriteTimeUtc(this.Name);
        }
    }
}
