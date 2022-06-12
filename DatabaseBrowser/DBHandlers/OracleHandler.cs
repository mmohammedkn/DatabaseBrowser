using Oracle.ManagedDataAccess.Client;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser.DBHandlers
{
    public class OracleHandler : IDBHandler
    {
        public OracleConnection OracleConnection;
        public SavedConnection _connection;

        OracleConnectionStringBuilder ConStrBuilder;
        OracleTransaction Transaction;

        public DbConnection Connect(SavedConnection connection)
        {
            ConStrBuilder = new OracleConnectionStringBuilder(connection.ConnectionString);
            OracleConnection = new OracleConnection(ConStrBuilder.ConnectionString);

            OracleConnection.StateChange += OracleConnection_StateChange;
            OracleConnection.InfoMessage += OracleConnection_InfoMessage;
            OracleConnection.Open();

            return OracleConnection;
        }

        private void OracleConnection_InfoMessage(object sender, OracleInfoMessageEventArgs eventArgs)
        {
            
        }

        private void OracleConnection_StateChange(object sender, StateChangeEventArgs e)
        {
            
        }

        public DataTable ExecuteSqlCommand(string sqlCommand)
        {
            DataTable Dt = new DataTable();

            if (OracleConnection == null)
            {
                OracleConnection = new OracleConnection(ConStrBuilder.ConnectionString);
            }

            OracleCommand DbCommand = new OracleCommand(sqlCommand, OracleConnection);
            OracleDataAdapter adtp = new OracleDataAdapter(DbCommand);

            try
            {
                adtp.Fill(Dt);
            }
            catch (Exception ex)
            {
                //var objError = GetObjectError();

                MessageBox.Show(Application.OpenForms[0], ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return Dt;
        }

        public void ExecuteSqlCommandInTransaction(string sqlCommand)
        {
            if (Transaction == null)
            {
                //Transaction = new OracleTransaction();
            }

            OracleCommand DbCommand = new OracleCommand(sqlCommand, OracleConnection) { Transaction = Transaction };
            DbCommand.ExecuteNonQuery();
        }

        public List<string> GetTableNames()
        {
            return ExecuteSqlCommand("SELECT table_name FROM dba_tables where UPPER(OWNER) = '" + ConStrBuilder.UserID.ToUpper() + "' order by table_name").AsEnumerable()
                .Select(x => x["table_name"].ToString()).ToList();
        }

        public List<string> GetViewNames()
        {
            return ExecuteSqlCommand("SELECT view_name FROM dba_views where UPPER(OWNER) = '" + ConStrBuilder.UserID.ToUpper() + "' order by view_name").AsEnumerable()
                .Select(x => x["view_name"].ToString()).ToList();
        }

        public List<string> GetUserNames()
        {
            return ExecuteSqlCommand("SELECT * FROM DBA_USERS order by username").AsEnumerable()
                            .Select(x => x["username"].ToString()).ToList();
        }

        public List<string> GetObjectColumnNames(string objectName)
        {
            var columns = ExecuteSqlCommand("SELECT * FROM SYS.ALL_TAB_COLUMNS where TABLE_NAME = '" + objectName
                + "' and UPPER(OWNER) = '" + ConStrBuilder.UserID.ToUpper() + "' order by COLUMN_ID");

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
            var columns = new List<string>();// GetObjectColumnNames(objectName);

            columns.Insert(0, "T.*");

            if (type == "Table")
            {
                columns.Insert(0, "ROWID row_id");
                columns.Insert(0, "ROWNUM row_num");
            }

            var query = ("select " + string.Join(",", columns) + " from " + objectName + " T").Replace("'", '"' + "");
            //var query = ("select * from (select " + string.Join(",", columns) + " from " + objectName + " T) where row_num > " + ((page - 1) * itemCount) + " and row_num <= " + (page * itemCount)).Replace("'", '"' + "");

            var ObjectData = ExecuteSqlCommand(query);
            ObjectData.TableName = objectName;

            if (type == "Table")
            {
                ObjectData.Columns["row_id"].ReadOnly = true;
                ObjectData.Columns["row_num"].ReadOnly = true;
            }

            return ObjectData;
        }

        public List<string> GetPackagesNames()
        {
            return ExecuteSqlCommand("SELECT OBJECT_NAME FROM ALL_OBJECTS WHERE OBJECT_TYPE IN 'PACKAGE' and UPPER(OWNER) = '" + ConStrBuilder.UserID.ToUpper() + "' order by OBJECT_NAME").AsEnumerable()
                .Select(x => x["OBJECT_NAME"].ToString()).ToList();
        }

        public string GetObjectSource(string objectName)
        {
            return String.Join("", ExecuteSqlCommand("select text from all_source where name = '" + objectName + "' and type = 'PACKAGE BODY' order by line")
                .AsEnumerable().Select(x => x["TEXT"].ToString()).ToList());
        }

        public void SaveChanges(DataTable dataTable)
        {
            foreach (DataRow row in dataTable?.Rows)
            {
                for (int y = 0; y < dataTable.Columns.Count; y++)
                {
                    if (row.RowState == DataRowState.Modified
                        && !row[y, DataRowVersion.Current].Equals(row[y, DataRowVersion.Original]))
                    {
                        UpdateRow(dataTable.TableName, dataTable.Columns[y].ColumnName, row["row_id"].ToString(), row[y]);
                    }
                }
            }
            dataTable.AcceptChanges();
        }

        private void UpdateRow(string tableName, string columnName, string rowId, object newValue)
        {
            ExecuteSqlCommand(
                "update " + tableName +
                " set " + columnName + " = '" + newValue + "'" +
                " where ROWID = '" + rowId + "'");

            ExecuteSqlCommand("commit");
        }

        ~OracleHandler()
        {
            OracleConnection.Close();
            OracleConnection.Dispose();
        }
    }
}
