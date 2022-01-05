using Oracle.ManagedDataAccess.Client;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseBrowser.DBHandlers
{
    public interface IDBHandler
    {
        DbConnection Connect(SavedConnection connection);
        DataTable ExecuteSqlCommand(string sqlCommand);
        void ExecuteSqlCommandInTransaction(string sqlCommand);
        List<string> GetTableNames();
        List<string> GetViewNames();
        List<string> GetObjectColumnNames(string objectName);
        decimal GetObjectRowCount(string objectName);
        DataTable GetObjectData(string objectName, int page, int itemCount, string type);
        List<string> GetPackagesNames();
        string GetObjectSource(string objectName);
        void SaveChanges(DataTable dataTable);
    }
}
