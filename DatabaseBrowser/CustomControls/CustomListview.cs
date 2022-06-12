using DatabaseBrowser.Forms;

using Microsoft.VisualBasic;

using Oracle.ManagedDataAccess.Client;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using static DatabaseBrowser.CustomControls.CustomListview;

using MessageBox = System.Windows.Forms.MessageBox;

namespace DatabaseBrowser.CustomControls
{
    public class CustomListview : ListView
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_CHANGEUISTATE = 0x127;
        private const int UIS_SET = 1;
        private const int UISF_HIDEFOCUS = 0x1;

        public DataTable TableData { get; set; }


        public CustomListview()
        {
            this.View = View.Details;
            this.FullRowSelect = true;
            this.ColumnClick += listView_ColumnClick;
            this.HideSelection = false;

            this.MouseDoubleClick += CustomListview_MouseDoubleClick;
        }

        private void CustomListview_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = this.HitTest(e.X, e.Y);
            ListViewItem item = info.Item;

            if (item != null)
            {
                var dataRow = (item.Tag as DataRow);
                var tableName = dataRow.Table.TableName;
                var subItem = info.SubItem;
                var columIndex = item.SubItems.IndexOf(subItem);
                var column = this.Columns[columIndex];
                var oldValue = subItem.Text;

                if (TableData == null)
                    TableData = dataRow.Table;

                var edit = Interaction.InputBox("Edit '" + column.Text + "'", "", oldValue);

                if (edit.DialogResult == DialogResult.OK && edit.Value != oldValue)
                {
                    try
                    {
                        dataRow[column.Text] = edit.Value;
                        
                        subItem.Text = edit.Value;

                        subItem.BackColor = Color.Green;
                    }
                    catch (Exception ex)
                    {
                        dataRow.RejectChanges();
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            this.GetType()
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(this, true, null);

            SetWindowTheme(this.Handle, "Explorer", null);

            SendMessage(this.Handle, WM_CHANGEUISTATE, MakeLong(UIS_SET, UISF_HIDEFOCUS), 0);
        }

        protected override bool ShowFocusCues => false;

        private int MakeLong(int wLow, int wHigh)
        {
            int low = (int)IntLoWord(wLow);
            short high = IntLoWord(wHigh);
            int product = 0x10000 * (int)high;
            int mkLong = (int)(low | product);
            return mkLong;
        }

        private short IntLoWord(int word)
        {
            return (short)(word & short.MaxValue);
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer sorter = GetListViewSorter(e.Column);

            this.ListViewItemSorter = sorter;
            this.Sort();
        }

        public enum ColumnDataType
        {
            String,
            Decimal,
            DateTime,
            Short,
            Long,
            Int
        }

        private ListViewItemComparer GetListViewSorter(int columnIndex)
        {
            ListViewItemComparer sorter = (ListViewItemComparer)this.ListViewItemSorter;
            if (sorter == null)
                sorter = new ListViewItemComparer();

            sorter.ColumnIndex = columnIndex;

            string columnName = this.Columns[columnIndex].Tag as string;
            switch (columnName)
            {
                case "Int32":
                    sorter.ColumnType = ColumnDataType.Int;
                    break;
                case "DateTime":
                    sorter.ColumnType = ColumnDataType.DateTime;
                    break;
                case "String":
                default:
                    sorter.ColumnType = ColumnDataType.String;
                    break;
            }

            if (sorter.SortDirection == SortOrder.Ascending)
                sorter.SortDirection = SortOrder.Descending;
            else
                sorter.SortDirection = SortOrder.Ascending;

            return sorter;
        }

        ~CustomListview()
        {
            Debug.WriteLine("CustomListview Deconsruting...");
        }
    }

    public class ListViewItemComparer : IComparer
    {
        private int _columnIndex;
        public int ColumnIndex
        {
            get
            {
                return _columnIndex;
            }
            set
            {
                _columnIndex = value;
            }
        }

        private SortOrder _sortDirection;
        public SortOrder SortDirection
        {
            get
            {
                return _sortDirection;
            }
            set
            {
                _sortDirection = value;
            }
        }

        private ColumnDataType _columnType;
        public ColumnDataType ColumnType
        {
            get
            {
                return _columnType;
            }
            set
            {
                _columnType = value;
            }
        }


        public ListViewItemComparer()
        {
            _sortDirection = SortOrder.None;
        }

        public int Compare(object x, object y)
        {
            ListViewItem lviX = x as ListViewItem;
            ListViewItem lviY = y as ListViewItem;

            int result = -1;

            if (lviX == null && lviY == null)
            {
                result = 0;
            }
            else if (lviX == null)
            {
                result = -1;
            }

            else if (lviY == null)
            {
                result = 1;
            }

            try
            {
                switch (ColumnType)
                {
                    case ColumnDataType.DateTime:
                        DateTime xDt = Convert.ToDateTime(lviX.SubItems[ColumnIndex].Text);
                        DateTime yDt = Convert.ToDateTime(lviY.SubItems[ColumnIndex].Text);
                        result = DateTime.Compare(xDt, yDt);
                        break;

                    case ColumnDataType.Decimal:
                        Decimal xD = Convert.ToDecimal(lviX.SubItems[ColumnIndex].Text.Replace("$", string.Empty).Replace(",", string.Empty));
                        Decimal yD = Convert.ToDecimal(lviY.SubItems[ColumnIndex].Text.Replace("$", string.Empty).Replace(",", string.Empty));
                        result = Decimal.Compare(xD, yD);
                        break;
                    case ColumnDataType.Short:
                        short xShort = Convert.ToInt16(lviX.SubItems[ColumnIndex].Text);
                        short yShort = Convert.ToInt16(lviY.SubItems[ColumnIndex].Text);
                        result = xShort.CompareTo(yShort);
                        break;
                    case ColumnDataType.Int:
                        int xInt = Convert.ToInt32(lviX.SubItems[ColumnIndex].Text);
                        int yInt = Convert.ToInt32(lviY.SubItems[ColumnIndex].Text);
                        return xInt.CompareTo(yInt);
                    case ColumnDataType.Long:
                        long xLong = Convert.ToInt64(lviX.SubItems[ColumnIndex].Text);
                        long yLong = Convert.ToInt64(lviY.SubItems[ColumnIndex].Text);
                        return xLong.CompareTo(yLong);
                    default:
                        result = string.Compare(
                            lviX.SubItems[ColumnIndex].Text,
                            lviY.SubItems[ColumnIndex].Text,
                            false);

                        break;
                }
            }
            catch
            {

            }

            if (SortDirection == SortOrder.Descending)
            {
                return -result;
            }
            else
            {
                return result;
            }
        }
    }
}
