using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DynamicsCrm.TestFramework
{
    public static class Extensions
    {
        public static byte[] Serialize(this object value)
        {
            DataContractSerializer serializer = new DataContractSerializer(value.GetType());

            // String writer prevents the BOM from being added
            using (var writer = new StringWriter())
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    OmitXmlDeclaration = false,
                    ConformanceLevel = ConformanceLevel.Document,
                    CheckCharacters = true
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
                {
                    serializer.WriteObject(xmlWriter, value);
                    xmlWriter.Flush();
                }

                return Encoding.UTF8.GetBytes(writer.ToString());
            }
        }

        public static T Deserialize<T>(this byte[] value)
        {
            using (MemoryStream stream = new MemoryStream(value))
            {
                //Serialize the Record object to a memory stream using DataContractSerializer.  
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }
    }
}
