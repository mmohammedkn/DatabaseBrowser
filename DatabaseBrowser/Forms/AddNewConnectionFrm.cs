using Krypton.Toolkit;
using Oracle.ManagedDataAccess.Client;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser
{
    public partial class AddNewConnectionFrm : KryptonForm
    {
        string connectionStringTemplate = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={2})(PORT={3})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={4})));User Id={0};Password={1};";
        string connectionStringTemplateSID = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={2})(PORT={3})))(CONNECT_DATA=(SERVER=DEDICATED)(SID={4})));User Id={0};Password={1};";
        private SavedConnection connection;

        public AddNewConnectionFrm()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            TestConnString(txtService.Text.Any() ? connectionStringTemplate : connectionStringTemplateSID);
        }

        private void TestConnString(string connectionString)
        {
            connectionString = string.Format(connectionString, txtUserId.Text, txtPassword.Text, txtHost.Text, txtPort.Text, txtService.Text.Any() ? txtService.Text : txtSID.Text);

            OracleConnection OC = new OracleConnection(connectionString);

            try
            {
                OC.Open();

                connection = new SavedConnection()
                {
                    Host = txtHost.Text,
                    Password = txtPassword.Text,
                    Port = Convert.ToInt32(txtPort.Text),
                    Service = txtService.Text.Any() ? txtService.Text : txtSID.Text,
                    UserId = txtUserId.Text
                };

                SavedConnection.AddConnection(connection);

                MessageBox.Show("Connection added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                if (connectionString.Contains("SERVICE_NAME"))
                {
                    TestConnString(connectionStringTemplate.Replace("SERVICE_NAME", "SID"));
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                OC.Dispose();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        internal void Open(Action<SavedConnection> value)
        {
            this.ShowDialog();
            value(connection);
        }
    }
}
