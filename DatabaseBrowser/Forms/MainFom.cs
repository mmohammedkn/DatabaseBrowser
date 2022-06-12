using DatabaseBrowser.CustomControls;
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser.Forms
{
    public partial class MainFom : KryptonForm
    {
        public MainFom()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            this.WindowState = FormWindowState.Maximized;

            toolStripMenuItem1.DropDownItems.AddRange(SavedConnection.GetConnections()
                .Select(x => GetConnectionDD(x)).ToArray());

            //KryptonDockingWorkspace w = kryptonDockingManager1.ManageWorkspace(wsConnections);
            //kryptonDockingManager1.ManageControl(kryptonPanel1, w);
            //kryptonDockingManager1.ManageFloating(this);

            //// Add docking pages
            //var left = kryptonDockingManager1.AddDockspace("Control", DockingEdge.Left,
            //    new KryptonPage[] { ObjectBrowser() });
            //kryptonDockingManager1.AddDockspace("Control", DockingEdge.Bottom, new KryptonPage[] { NewDocument() });
            //kryptonDockingManager1.AddToWorkspace("Workspace", new KryptonPage[] { NewDocument() });
        }

        private ToolStripMenuItem GetConnectionDD(SavedConnection x)
        {
            var toolstripItem = new ToolStripMenuItem()
            {
                Text = x.ToString(),
                Tag = x,
                AutoSize = true,
            };

            toolstripItem.Click += ToolstripItem_Click;

            return toolstripItem;
        }

        private void ToolstripItem_Click(object sender, EventArgs e)
        {
            AddConnection((sender as ToolStripMenuItem).Tag as SavedConnection);
        }

        private KryptonPage NewDocument()
        {
            KryptonPage p = new KryptonPage
            {
                Text = "Object Name",
            };
            p.TextTitle = p.Text;
            p.TextDescription = p.Text;
            p.UniqueName = p.Text;
            //p.ImageSmall = (Bitmap)imageListSmall.Images[0];

            return p;
        }

        private void AddConnection(SavedConnection connection)
        {
            if (connection == null)
                return;

            var conTab = new KryptonPage(connection.UserId + "@" + connection.Host);
            conTab.Controls.Add(new ConnectionTab(connection) { Dock = DockStyle.Fill }); ;
            connectionsTabs.Pages.Add(conTab);
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AddNewConnectionFrm().Open((SavedConnection connection) =>
            {
                AddConnection(connection);
            });
        }
    }
}
