using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser.DBHandlers
{
    public class SQLiteHandler : IDBHandler
    {
        SQLiteConnection SQLiteConnection;
        SQLiteConnectionStringBuilder ConnectionString;

        public OracleConnection OracleConnection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public SavedConnection _connection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DbConnection Connect(SavedConnection connection)
        {
            ConnectionString = new SQLiteConnectionStringBuilder(@"Data Source=" + connection.ConnectionString);
            SQLiteConnection = new SQLiteConnection(ConnectionString.ConnectionString);

            SQLiteConnection.Open();

            return SQLiteConnection;
        }

        public DataTable ExecuteSqlCommand(string sqlCommand)
        {
            DataTable Dt = new DataTable();

            if (SQLiteConnection == null)
            {
                SQLiteConnection = new SQLiteConnection(ConnectionString.ConnectionString);
            }

            if (SQLiteConnection.State == ConnectionState.Closed)
            {
                SQLiteConnection.Open();
            }

            SQLiteCommand DbCommand = new SQLiteCommand(sqlCommand, SQLiteConnection);
            SQLiteDataAdapter adtp = new SQLiteDataAdapter(DbCommand);

            try
            {
                adtp.Fill(Dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return Dt;
        }

        public void ExecuteSqlCommandInTransaction(string sqlCommand)
        {

        }

        public List<string> GetTableNames()
        {
            return ExecuteSqlCommand("SELECT name table_name FROM sqlite_master WHERE type = 'table' ORDER BY 1").AsEnumerable()
                .Select(x => x["table_name"].ToString()).ToList();
        }

        public List<string> GetViewNames()
        {
            return ExecuteSqlCommand("SELECT name view_name FROM sqlite_master WHERE type = 'view' ORDER BY 1").AsEnumerable()
                .Select(x => x["view_name"].ToString()).ToList();
        }

        public List<string> GetObjectColumnNames(string objectName)
        {
            var columns = ExecuteSqlCommand("SELECT * FROM SYS.ALL_TAB_COLUMNS where TABLE_NAME = '" + objectName
                + " order by COLUMN_ID");

            var columnNames = columns.AsEnumerable().Where(x => x["DATA_TYPE"].ToString() != "CLOB").Select(x => x["COLUMN_NAME"] as string);

            return columnNames.ToList();
        }

        public decimal GetObjectRowCount(string objectName)
        {
            var count = (decimal)ExecuteSqlCommand("SELECT count(*) Count from " + objectName).Rows[0]["Count"];
            return count;
        }

        public DataTable GetObjectData(string objectName, int page, int itemCount, string type)
        {
            var query = ("select * from (select * from " + objectName + ")").Replace("'", '"' + "");

            return ExecuteSqlCommand(query);
        }

        public List<string> GetPackagesNames()
        {
            throw new NotImplementedException();
        }

        public string GetObjectSource(string objectName)
        {
            throw new NotImplementedException();
        }

        public void SaveChanges(DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public List<string> GetUserNames()
        {
            throw new NotImplementedException();
        }

        ~SQLiteHandler()
        {
            SQLiteConnection.Close();
            SQLiteConnection.Dispose();
        }
    }
}
