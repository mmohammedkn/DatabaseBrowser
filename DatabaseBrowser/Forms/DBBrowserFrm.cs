using DatabaseBrowser.CustomControls;
using DatabaseBrowser.DBHandlers;

using FastColoredTextBoxNS;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser
{
    public partial class DBBrowserFrm : Form
    {
        List<SavedConnection> SavedConnections = new List<SavedConnection>();

        public DBBrowserFrm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            if (SavedConnection.GetConnections().Any())
            {
                foreach (SavedConnection item in SavedConnection.GetConnections())
                {
                    var connectionItem = menuConnections.DropDownItems.Add(item.UserId + "@" + item.SID);
                    connectionItem.Tag = item;
                    connectionItem.Click += connectionItem_Click;
                }
            }
            else
            {
                new AddNewConnectionFrm().Show();
            }
        }

        #region Methods

        void ConnectToDB(SavedConnection connection, DBTypes type)
        {
            IDBHandler IDBHandler;

            switch (type)
            {
                case DBTypes.Oracle:
                default:
                    IDBHandler = new OracleHandler();
                    IDBHandler.Connect(connection);
                    break;
                case DBTypes.SqlLite:
                    IDBHandler = new SQLiteHandler();
                    IDBHandler.Connect(connection);
                    break;
            }

            this.BeginInvoke(new Action(() =>
            {
                AddDBConToUI(IDBHandler, connection);
            }));
        }

        private void AddDBConToUI(IDBHandler dBHandler, SavedConnection connection)
        {
            TabPage tabPage = new TabPage(connection.UserId + "@" + connection.Host + @"/" + connection.SID);
            DBConnectionTab dbConTab = new DBConnectionTab(dBHandler);
            tabPage.Controls.Add(dbConTab);
            tabPage.Tag = dbConTab;

            dbConTab.Dock = DockStyle.Fill;

            dbConTab.treeObjects.AfterExpand += treeView1_AfterExpand;
            dbConTab.treeObjects.AfterSelect += treeView1_AfterSelect;

            dbConTab.treeObjects.Nodes.Clear();

            dbConTab.treeObjects.Nodes.Add("table_name", "Tables");
            dbConTab.treeObjects.Nodes.Add("view_name", "Views");
            dbConTab.treeObjects.Nodes.Add("package_name", "Packages");
            dbConTab.treeObjects.Nodes.Add("user_name", "Users");

            foreach (TreeNode item in dbConTab.treeObjects.Nodes)
                item.Nodes.Add("");

            tabDBConns.TabPages.Add(tabPage);
            tabDBConns.SelectedTab = tabPage;
        }

        #endregion

        #region Events

        void connectionItem_Click(object sender, EventArgs e)
        {
            new Task(() =>
            {
                ConnectToDB((sender as ToolStripItem).Tag as SavedConnection, DBTypes.Oracle);
            }).Start();
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            e.Node.Nodes.Clear();

            DBConnectionTab dbConTab = tabDBConns.SelectedTab.Tag as DBConnectionTab;

            new Task(() =>
            {
                List<string> objectsNames = new List<string>();
                if (e.Node.Text == "Tables")
                    objectsNames = dbConTab.dBHandler.GetTableNames();
                else if (e.Node.Text == "Views")
                    objectsNames = dbConTab.dBHandler.GetViewNames();
                else if (e.Node.Text == "Packages")
                    objectsNames = dbConTab.dBHandler.GetPackagesNames();
                else if (e.Node.Text == "Users")
                    objectsNames = dbConTab.dBHandler.GetUserNames();

                this.BeginInvoke(new Action(() =>
                {
                    dbConTab.treeObjects.BeginUpdate();

                    foreach (var item in objectsNames)
                        e.Node.Nodes.Add(item);

                    dbConTab.treeObjects.EndUpdate();
                }));
            }).Start();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null)
                return;

            DBConnectionTab dbConTab = tabDBConns.SelectedTab.Tag as DBConnectionTab;

            var found = dbConTab.tabObjects.TabPages.Cast<TabPage>().Where(x => x.Text == e.Node.Text);
            if (found.Any())
            {
                dbConTab.tabObjects.SelectedTab = found.First();
                return;
            }

            var tabPage = new TabPage(e.Node.Text);
            dbConTab.tabObjects.TabPages.Add(tabPage);

            FastColoredTextBox currentTabTB = null;
            CustomListview currentTabGC = null;

            if (e.Node.Parent.Text == "Tables" || e.Node.Parent.Text == "Views")
            {
                currentTabGC = new CustomListview
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };
                tabPage.Controls.Add(currentTabGC);

                currentTabGC.Tag = new GridInfo() { CurrentPage = 1, ObjectName = e.Node.Text };
            }
            else if (e.Node.Parent.Text == "Packages")
            {
                currentTabTB = new FastColoredTextBox
                {
                    Language = Language.SQL,
                    Dock = DockStyle.Fill,
                };
                currentTabTB.TextChanged += CurrentTabTB_TextChanged;

                tabPage.Controls.Add(currentTabTB);
            }

            dbConTab.tabObjects.SelectedTab = tabPage;

            new Thread(() =>
            {
                if (e.Node.Parent.Text == "Tables" || e.Node.Parent.Text == "Views")
                    LoadGridData(currentTabGC, e.Node.Text, e.Node.Parent.Text);
                else if (e.Node.Parent.Text == "Packages")
                    LoadObjectSource(currentTabTB, e.Node.Text);

            }).Start();
        }

        private void CurrentTabTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            e.ChangedRange.ClearFoldingMarkers();
        }

        private void LoadObjectSource(FastColoredTextBox currentTabTB, string objectName)
        {
            DBConnectionTab dbConTab = tabDBConns.SelectedTab.Tag as DBConnectionTab;
            var sourceText = dbConTab.dBHandler.GetObjectSource(objectName);

            this.BeginInvoke(new Action(() =>
            {
                var pkgObjects = Regex.Match(sourceText, @"PROCEDURE|FUNCTION\s+(\w+)").Groups;
                currentTabTB.Text = sourceText;
            }));
        }

        private void LoadGridData(CustomListview gridControl, string objectName, string type)
        {
            GridInfo info = gridControl.Tag as GridInfo;
            DBConnectionTab dbConTab = tabDBConns.SelectedTab.Tag as DBConnectionTab;

            var data = dbConTab.dBHandler.GetObjectData(objectName, info.CurrentPage, 100, type);

            if (data.Columns.Count == 0)
            {
                return;
            }

            gridControl.BeginInvoke(new Action(() =>
            {
                gridControl.BeginUpdate();

                var skip = type == "Views" ? 0 : 2;

                foreach (var item in data.Columns.Cast<DataColumn>().Skip(skip))
                {
                    var col = gridControl.Columns.Add(item.ColumnName);
                    col.Tag = item.DataType.Name;
                }

                foreach (var item in data.Rows.Cast<DataRow>())
                {
                    var listviewItem = new ListViewItem(item[skip].ToString());
                    listviewItem.UseItemStyleForSubItems = false;

                    for (int i = skip + 1; i < data.Columns.Count; i++)
                        listviewItem.SubItems.Add(item[i].ToString());

                    listviewItem.Tag = item;

                    gridControl.Items.Add(listviewItem);
                }

                foreach (var item in gridControl.Columns.Cast<ColumnHeader>())
                    item.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);

                gridControl.EndUpdate();
            }));
        }

        void DBBrowserFrm_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            DBConnectionTab dbConTab = tabDBConns.SelectedTab.Tag as DBConnectionTab;

            var newValue = e.Column.DataType == typeof(String) ? "'" + e.ProposedValue + "'" : e.ProposedValue;
            dbConTab.dBHandler.ExecuteSqlCommand("update " + e.Row.Table.TableName + " set " + e.Column.ColumnName + " = " + newValue + " where row_id = '" + e.Row["row_id"] + "'");
        }

        void DBBrowserFrm_RowChanged(object sender, DataRowChangeEventArgs e)
        {

        }

        private void DBBrowserFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (OC != null)
            //{
            //    OC.Dispose();
            //}
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DBConnectionTab dBConnectionTab = (tabDBConns.SelectedTab.Controls[0] as DBConnectionTab);
            var item = dBConnectionTab.tabObjects.SelectedTab.Controls[0];
            if (item is CustomListview)
            {
                var listview = (CustomListview)item;
                if (listview.TableData == null)
                    return;

                var changes = listview.TableData.GetChanges(DataRowState.Modified);
                dBConnectionTab.dBHandler.SaveChanges(changes);
            }
        }

        private void newConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AddNewConnectionFrm().Show();
        }

        private void sQLLiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "SqlLite DB files|*.db";
            dialog.ShowDialog(this);

            //ConnectToDB(dialog.FileName, DBTypes.SqlLite);

            //var OC = new SQLiteConnection(@"Data Source=" + dialog.FileName);

            //SQLiteDataAdapter ad;
            //DataTable dt = new DataTable();

            //try
            //{
            //    SQLiteCommand cmd;
            //    OC.Open();  //Initiate connection to the db
            //    cmd = (OC as SQLiteConnection).CreateCommand();
            //    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1";  //set the passed query
            //    ad = new SQLiteDataAdapter(cmd);
            //    ad.Fill(dt); //fill the datasource
            //}
            //catch (SQLiteException ex)
            //{
            //    //Add your exception code here.
            //}
            //OC.Close();
        }

        #endregion Events

        enum DBTypes
        {
            Oracle,
            SqlLite
        }

        class GridInfo
        {
            public string ObjectName { get; set; }
            public int CurrentPage { get; set; }
        }
    }
}
