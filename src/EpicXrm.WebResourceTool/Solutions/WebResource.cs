using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EpicXrm.WebResourceTool.Solutions
{
    [Serializable]
    public class WebResource
    {
        public Guid WebResourceId { get; set; } = Guid.Empty;
        public bool RemoveExtension { get; set; } = false;
        public int LanguageCode { get; set; } = 1033;
        public bool IsCustomizable { get; set; } = true;
        public bool CanBeDeleted { get; set; } = true;
        public bool IsHidden { get; set; } = false;
        public bool Delete { get; set; } = false;
        public string Checksum { get; set; } = string.Empty;
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int WebResourceType { get; set; }

        public string FileName
        {
            get
            {
                return "/WebResources/" + this.Name.Replace("//", "").Replace(".", "") + this.WebResourceId.ToString();
            }
        }

        public bool FileExists()
        {
            Delete = !File.Exists(Name);
            return File.Exists(Name);
        }

        public void UpdateChecksum()
        {
            Checksum = GetCurrentChecksum();
        }

        public string GetCurrentChecksum()
        {
            using (HashAlgorithm hashAlgo = SHA256.Create())
            {
                using (var stream = File.OpenRead(Name))
                {
                    var hash = hashAlgo.ComputeHash(stream);
                    StringBuilder stringBuilder = new StringBuilder();

                    foreach (byte b in hash)
                    {
                        stringBuilder.AppendFormat("{0:X2}", b);
                    }

                    return stringBuilder.ToString();
                }
            }
        }
    }
}
