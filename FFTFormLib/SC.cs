using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharp;
using SPInterface;
using PdfSharp.Pdf;
using System.Data;

namespace FFTFormLib
{
    public static class SC
    {
        public static string outputPath = "";                    //PDF report save path
        public static Dictionary<string, string> sysPara;   //sysPara from the Calypso
        public static PdfDocument document;                //pointer for each single element generate a page in this document
        public static bool exportPDF = false;
        public static double ZPRange;
        public static SPI sp_f;
        public static int no_result;
        public static int tol_mode;
        public static double R;
        public static double n0;
        public static double k;
        public static DataSet ds;
        public static string filepath = System.AppDomain.CurrentDomain.BaseDirectory;

        static SC()
        {
            ds = new DataSet("Lead_ds");
            string filename = System.IO.Path.Combine(filepath, "lead.xml");
            if (System.IO.File.Exists(filename))
            {
                ds.ReadXml(filename);
            }

            if (!ds.Tables.Contains("lead"))
            {
                DataTable lead = ds.Tables.Add("lead");
                DataColumn lead_id = new DataColumn("lead_id");
                lead_id.Unique = true;
                lead_id.DataType = System.Type.GetType("System.String");

                lead.Columns.Add(lead_id);
                lead.Columns.Add("lead_value", typeof(Int32));
            }

        }
    }

}
