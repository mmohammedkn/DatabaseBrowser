using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DatabaseBrowser
{
    public class SavedConnection
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Service { get; set; }
        public string SID { get; set; }
        public string ConnectionString
        {
            get
            {
                string connectionStringServiceTemplate = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={2})(PORT={3})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={4})));User Id={0};Password={1};";
                string connectionStringSIDTemplate = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={2})(PORT={3})))(CONNECT_DATA=(SERVER=DEDICATED)(SID={4})));User Id={0};Password={1};";
                string connectionString = string.Format(string.IsNullOrEmpty(Service) ? connectionStringSIDTemplate : connectionStringServiceTemplate, UserId, Password, Host, Port, string.IsNullOrEmpty(Service) ? SID : Service);
                return connectionString;
            }
        }

        static JavaScriptSerializer JSSerializer = new JavaScriptSerializer();
        public static void AddConnection(SavedConnection Connection)
        {
            List<SavedConnection> currentConnections = GetConnections();
            currentConnections.Add(Connection);
            File.WriteAllText("SavedConnections.db", JSSerializer.Serialize(currentConnections));
        }

        public static void RemoveConnection(SavedConnection Connection)
        {
            List<SavedConnection> currentConnections = GetConnections();
            currentConnections.Remove(Connection);
            File.WriteAllText("SavedConnections.db", JSSerializer.Serialize(currentConnections));
        }

        public static List<SavedConnection> GetConnections()
        {
            List<SavedConnection> SavedConnections = new List<SavedConnection>();

            if (File.Exists("SavedConnections.db"))
            {
                SavedConnections = JSSerializer.Deserialize<List<SavedConnection>>(File.ReadAllText("savedConnections.db"));
            }

            return SavedConnections;
        }

        public override string ToString()
        {
            return UserId + "@" + Host + "/" + (Service != null ? Service : SID);
        }
    }
}
