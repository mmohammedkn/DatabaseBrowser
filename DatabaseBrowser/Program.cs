using SpreadsheetLight;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseBrowser
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //SLDocument document = new SLDocument(@"C:\Users\Mazen.Mohammed\Downloads\9be37679-4bfb-4155-96b9-f0cc4137c590.xlsx");

            //var rowCount = document.GetWorksheetStatistics().EndRowIndex - 8;

            //var Data = Enumerable.Range(8, rowCount).Select(x => new
            //{
            //    TransactionDate = document.GetCellValueAsDateTime(x, 2, "dd-MM-yyyy"),
            //    Description = document.GetCellValueAsString(x, 3),
            //    Amount = Double.Parse(document.GetCellValueAsString(x, 6)),
            //}).Where(x => x.Amount < 0 && x.Description != "Insurance premium").Select(x => new
            //{
            //    Description = x.Description.Replace("Purchase, ",""),
            //    x.Amount,
            //    DueDate = x.TransactionDate.AddDays(55),
            //    RemainingDays = Math.Round((x.TransactionDate.AddDays(55) - DateTime.Now).TotalDays)
            //}).OrderBy(x => x.DueDate).Where(x => x.DueDate >= DateTime.Now).ToList();

            Application.Run(new DBBrowserFrm());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message);
        }
    }
}
