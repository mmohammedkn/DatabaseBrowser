using DatabaseBrowser.DBHandlers;
using DatabaseBrowser.Models;
using FastColoredTextBoxNS;
using Krypton.Docking;
using Krypton.Navigator;
using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace DatabaseBrowser.CustomControls
{
    public partial class ConnectionTab : UserControl
    {
        SavedConnection _dbConnection;
        OracleHandler oracleHandler = new OracleHandler();

        public ConnectionTab(SavedConnection dbConnection)
        {
            InitializeComponent();

            _dbConnection = dbConnection;

            oracleHandler.Connect(dbConnection);

            KryptonDockingWorkspace w = kryptonDockingManager1.ManageWorkspace(kryptonDockableWorkspace1);
            kryptonDockingManager1.ManageControl(kryptonPanel1, w);
            var left = kryptonDockingManager1.AddDockspace("Control", DockingEdge.Left,
                new KryptonPage[] { ObjectBrowser() });
        }

        private KryptonPage ObjectDescription(DBObject dbObj)
        {
            var objectType = dbObj.Type.ToString();
            var objectName = dbObj.Name;

            KryptonPage p = new KryptonPage
            {
                Text = objectType.TrimEnd('s') + " : " + objectName,
                MinimumSize = new Size(200, 0),
            };

            if (objectType == "Table")
            {
                KryptonDataGridView gridView = new KryptonDataGridView();
                gridView.Dock = DockStyle.Fill;
                gridView.CellEndEdit += GridView_CellEndEdit;
                gridView.NewRowNeeded += GridView_NewRowNeeded;
                gridView.CellBeginEdit += GridView_CellBeginEdit;
                gridView.CellDoubleClick += GridView_CellDoubleClick;
                p.Controls.Add(gridView);

                dbObj.Type = DBType.Table;

                new Task(() => { LoadTableData(gridView, dbObj); }).Start();
            }
            else if (objectType == "View")
            {
                KryptonDataGridView gridView = new KryptonDataGridView();
                gridView.Dock = DockStyle.Fill;
                p.Controls.Add(gridView);

                dbObj.Type = DBType.View;

                new Task(() => { LoadTableData(gridView, dbObj); }).Start();
            }
            else if (objectType == "Package")
            {
                FastColoredTextBox richTextBox = new FastColoredTextBox();
                richTextBox.Dock = DockStyle.Fill;
                richTextBox.Language = Language.SQL;
                p.Controls.Add(richTextBox);

                dbObj.Type = DBType.Package;

                new Task(() => { LoadPackageSource(richTextBox, dbObj); }).Start();
            }

            p.Tag = dbObj;

            return p;
        }

        private void GridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var grid = sender as KryptonDataGridView;
            var column = grid.Columns[e.ColumnIndex];
            var value = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            var dt = (grid.DataSource as DataTable);
            oracleHandler.SaveChanges(dt);
        }

        private void GridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = sender as KryptonDataGridView;
            grid.BeginEdit(true);
        }

        object oldValue = null;
        private void GridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var grid = sender as KryptonDataGridView;
            var column = grid.Columns[e.ColumnIndex];
            var value = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

            oldValue = value;
        }

        private void GridView_NewRowNeeded(object sender, DataGridViewRowEventArgs e)
        {

        }

        private void LoadTableData(KryptonDataGridView gridView, DBObject dbObject)
        {
            var data = oracleHandler.GetObjectData(dbObject.Name, 0, 0, dbObject.Type.ToString());

            gridView.BeginInvoke(new Action(() =>
            {
                gridView.DataSource = data;

                if (gridView.Columns.Contains("row_id"))
                {
                    gridView.Columns["row_id"].Visible = false;
                    gridView.Columns["row_num"].Visible = false;
                }

                gridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                gridView.AllowUserToOrderColumns = true;
            }));
        }

        private void LoadPackageSource(FastColoredTextBox richTextBox, DBObject dbObject)
        {
            var objectSource = oracleHandler.GetObjectSource(dbObject.Name);

            richTextBox.BeginInvoke(new Action(() =>
            {
                richTextBox.Text = objectSource;
            }));
        }

        private KryptonPage ObjectBrowser()
        {
            KryptonPage page = new KryptonPage
            {
                Text = "Schema Browser",
                MinimumSize = new Size(300, 0),
            };
            page.TextTitle = page.Text;
            page.TextDescription = page.Text;
            page.UniqueName = page.Text;
            //p.ImageSmall = (Bitmap)imageListSmall.Images[1];

            var conMenu = new KryptonContextMenu();
            KryptonContextMenuItems customItems = new KryptonContextMenuItems();
            customItems.Items.Add(new KryptonContextMenuItem("Pin To Top"));
            conMenu.Items.Add(customItems);

            var ObjectBrowser = new CustomKryptonTreeView()
            {
                Dock = DockStyle.Fill,
                KryptonContextMenu = conMenu,
                HideSelection = false,
            };
            ObjectBrowser.AfterExpand += ObjectBrowser_AfterExpand;
            ObjectBrowser.AfterSelect += ObjectBrowser_AfterSelect;

            var Tables = ObjectBrowser.Nodes.Add("Tables");
            var Views = ObjectBrowser.Nodes.Add("Views");
            var Packages = ObjectBrowser.Nodes.Add("Packages");

            Tables.Nodes.Add("Loading...");
            Views.Nodes.Add("Loading...");
            Packages.Nodes.Add("Loading...");

            page.Controls.Add(ObjectBrowser);

            page.ClearFlags(KryptonPageFlags.DockingAllowClose);

            return page;
        }

        private void ObjectBrowser_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null)
                return;

            var alreadyExist = kryptonDockableNavigator1.Pages.FirstOrDefault(x => (x.Tag as DBObject).Equals((e.Node.Tag as DBObject)));
            if (alreadyExist != null)
            {
                kryptonDockableNavigator1.SelectedPage = alreadyExist;
                return;
            }

            var page = ObjectDescription(e.Node.Tag as DBObject);

            kryptonDockableNavigator1.Pages.Add(page);

            ButtonSpecAny bs = new ButtonSpecAny
            {
                Type = PaletteButtonSpecStyle.Close,
            };

            page.ButtonSpecs.Add(bs);

            kryptonDockableNavigator1.SelectedPage = page;

            kryptonDockableNavigator1.MouseClick += KryptonDockableNavigator1_MouseClick;

            bs.Click += (a, s) => { kryptonDockableNavigator1.PerformCloseAction(page); };
        }

        private void KryptonDockableNavigator1_MouseClick(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Middle && focusedPage != null
            //    && kryptonDockableNavigator1.Pages.Contains(focusedPage))
            //{
            //    kryptonDockableNavigator1.Pages.Remove(focusedPage);
            //}
        }

        private void ObjectBrowser_AfterExpand(object sender, TreeViewEventArgs e)
        {
            new Task(() => { FillObjectTree(e.Node); }).Start();
        }

        private void FillObjectTree(TreeNode node)
        {
            List<string> Items = new List<string>();
            DBType dbType = DBType.Table;
            if (node.Text == "Tables")
            {
                Items = oracleHandler.GetTableNames();
                dbType = DBType.Table;
            }
            else if (node.Text == "Views")
            {
                Items = oracleHandler.GetViewNames();
                dbType = DBType.View;
            }
            else if (node.Text == "Packages")
            {
                Items = oracleHandler.GetPackagesNames();
                dbType = DBType.Package;
            }

            node.TreeView.BeginInvoke(new Action(() =>
            {
                node.Nodes.Clear();
                foreach (var item in Items)
                {
                    var tvNode = node.Nodes.Add(item);
                    tvNode.Tag = new DBObject() 
                    { 
                        Name = item, 
                        Type = dbType, 
                        DBConnection = _dbConnection 
                    };
                }
            }));
        }
    }
}
