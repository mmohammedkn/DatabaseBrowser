using DatabaseBrowser.DBHandlers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

using Control = System.Windows.Forms.Control;
using UserControl = System.Windows.Forms.UserControl;

namespace DatabaseBrowser.CustomControls
{
    public partial class DBConnectionTab : UserControl
    {
        public IDBHandler dBHandler;

        public DBConnectionTab(IDBHandler dBHandler)
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            this.dBHandler = dBHandler;
        }

        private void tabObjects_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                Rectangle mouseRect = new Rectangle(e.X, e.Y, 1, 1);
                for (int i = 0; i < tabObjects.TabCount; i++)
                {
                    if (tabObjects.GetTabRect(i).IntersectsWith(mouseRect))
                    {
                        foreach (var item in tabObjects.TabPages[i].Controls.Cast<Control>())
                        {
                            if (item is CustomListview)
                            {
                                var listview = (CustomListview)item;
                                if (listview.TableData == null)
                                    continue;
                                
                                var changes = listview.TableData.GetChanges(DataRowState.Modified);
                                dBHandler.SaveChanges(changes);
                            }

                            if (item is IDisposable)
                                (item as IDisposable).Dispose();

                            tabObjects.TabPages[i].Controls.Remove(item);
                        }

                        tabObjects.TabPages.Remove(tabObjects.TabPages[i]);
                        GC.Collect();
                        break;
                    }
                }
            }
        }
    }
}
