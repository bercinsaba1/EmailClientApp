using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization; // Add this for XmlSerializer
using System.IO; // Add this for StreamWriter and StreamReader

namespace Erasmus
{
    public class DataService
    {
        public void SaveData(string filename, EmailFolder rootFolder)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(EmailFolder));
            using (StreamWriter writer = new StreamWriter(filename))
            {
                serializer.Serialize(writer, rootFolder);
            }
        }

        public EmailFolder LoadData(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(EmailFolder));
            using (StreamReader reader = new StreamReader(filename))
            {
                return (EmailFolder)serializer.Deserialize(reader);
            }
        }
    }
}
 