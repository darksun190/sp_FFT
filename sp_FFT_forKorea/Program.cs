using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPInterface;
using PdfSharp;
using PdfSharp.Pdf;
using System.Threading;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Diagnostics;
using FFTFormLib;

namespace sp_FFT_forKorea
{

    static class Program
    {
        private static bool isaxial;
        /// <summary>
        /// main entrance for the program
        /// </summary>
        [STAThread]
        static void Main()
        {

            String[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length == 0 || arguments[1] != "axial")
            {
                isaxial = false;
            }
            else
            {
                isaxial = true;
            }
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //get parameters from app.config
            SC.exportPDF = Convert.ToBoolean(ConfigurationManager.AppSettings["outputPDF"]);
            SC.ZPRange = Convert.ToDouble(ConfigurationManager.AppSettings["ZeroPaddingRange"]) * Math.PI / 180;
            SC.no_result = Convert.ToInt32(ConfigurationManager.AppSettings["NoResult"]);
            SC.R = Convert.ToDouble(ConfigurationManager.AppSettings["R"]);
            SC.n0 = Convert.ToDouble(ConfigurationManager.AppSettings["n0"]);
            SC.k = Convert.ToDouble(ConfigurationManager.AppSettings["k"]);
            SC.tol_mode = Convert.ToInt32(ConfigurationManager.AppSettings["tol_mode"]);
            int PDFMODE = Convert.ToInt32(ConfigurationManager.AppSettings["PDF_PATH_MODE"]);
            int minPoints = Convert.ToInt32(ConfigurationManager.AppSettings["minPoints"]);
            string PDFpath1, PDFpath2, PDFpath3;        // 3 different path for select
            // 1 for PDF file path, 2 for default in Claypso, 3 for customer define
            PDFpath1 = "";
            PDFpath2 = "";
            PDFpath3 = "";


            //set the configuration file path, every thing depends on this file
            //usually just change a folder
            SPI a = new SPI(@"..\..\sp_FFT\conf\spiConf.xml");
            SC.sp_f = a;


            string inspectfilename = a.sys_dict["nameOfFile"] + @"\inspset";
            string inspset_content;
            if (File.Exists(inspectfilename))
            {
                inspset_content = System.IO.File.ReadAllText(inspectfilename);
            }
            else
            {
                inspset_content = "";
            }
            // try to get the pdf save path from the file "inspset";

            string pattern = @"(?<=#pdfFileAll ' ->' ')(.*)(?=')";          //search something like
            //#pdfFileAll ' ->' ' C:\programefiles\XXXX.pdf '
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            Match ma = rgx.Match(inspset_content);
            SC.sysPara = a.sys_dict;
            if (ma.Success)
            {
                string[] name_list = ma.Value.Split('+');
                foreach (string sn in name_list)
                {
                    string pa_gh = "(?<=getRecordHead\\(\").*(?=\")";       //try to analysis the header date from Claypso sys
                    Regex rgx_g = new Regex(pa_gh);
                    Match ma_g = rgx_g.Match(sn);
                    if (ma_g.Success)
                    {
                        if (a.sys_dict.ContainsKey(ma_g.Value))
                            PDFpath1 += a.sys_dict[ma_g.Value];
                    }
                    else
                    {
                        string pa_qu = "(?<=\").*(?=\")";
                        Regex rgx_q = new Regex(pa_qu);
                        Match ma_q = rgx_q.Match(sn);
                        PDFpath1 += ma_q.Value;
                    }
                }
                //MessageBox.Show(SC.outputPath);
                //MessageBox.Show(SC.outputPath);
                //calc the output path
                PDFpath1 = PDFpath1.Substring(0, PDFpath1.LastIndexOf('\\') + 1);   //only need the path, without the file names
                PDFpath1 += SC.sysPara["planid"] + "_" + SC.sysPara["partnbinc"] + ".pdf";
            }

            {
                //get the PDFpath2
                //try the default folder
                // save in the registry table
                //[HKEY_CURRENT_USER\Software\Zeiss\Application]
                //"LastUsed"="C:\\Program Files (x86)\\Zeiss\\CALYPSO 5.4\\"

                //get the calypso folder
                string currentPath = System.IO.Directory.GetCurrentDirectory();
                string configPath = currentPath + @"\config\env\values";    //only worked for less than 5.4
                string configcontent = System.IO.File.ReadAllText(configPath);
                string pattern_config = @"(?<=#defaultPathForTablefiles -> ').*?(?=')";
                Regex rgx_config = new Regex(pattern_config);
                Match ma_config = rgx_config.Match(configcontent);
                PDFpath2 = ma_config.Value + "\\";
                PDFpath2 += SC.sysPara["planid"] + "_" + SC.sysPara["partnbinc"] + ".pdf";
            }

            if (PDFMODE == 0)
            {
                SC.outputPath = PDFpath1 == String.Empty ? PDFpath2 : PDFpath1;

            }
            else if (PDFMODE == 1)
            {
                SC.outputPath = PDFpath1 == String.Empty ? PDFpath2 : PDFpath1;
            }
            else if (PDFMODE == 2)
            {
                SC.outputPath = PDFpath2 == String.Empty ? PDFpath1 : PDFpath2;
            }
            else if (PDFMODE == 3)
            {
                PDFpath3 = ConfigurationManager.AppSettings["customerPDFpath"];
                PDFpath3.Replace("%Calypso%", System.IO.Directory.GetCurrentDirectory());
                PDFpath3.Replace("%default%", PDFpath2);
                PDFpath3.Replace("%inspection%", SC.sysPara["planid"]);
                PDFpath3.Replace("%number%", SC.sysPara["partnbinc"]);
                SC.outputPath = PDFpath3;
            }
            //default windows size, same as PDF document

            Size window_size = new Size(1025, 850);
            SC.document = new PdfDocument();
            foreach (Element ele in a.elements)             //traversal all element
            {
                Cylinder cy;                                //only try cylinder and circle
                try
                {
                    if (!(ele.geoType == "Circle" || ele.geoType == "Cylinder"))
                    {
                        throw new Exception("wrong type of feature, this program only deal with Circle or Cylinder");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    continue;
                }
                if (ele.getMeasPoints().Count < minPoints)
                {
                    MessageBox.Show("point number is not enough in " + ele.identifier); //next element if not enough points
                    continue;
                }
                cy = new Cylinder(ele);
                try
                {
                    if (Math.Abs(cy.round_nr) < 1.33 * Math.PI)             //if enough angle, now is 240 degree
                    {
                        throw new Exception("Range is not enough");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    continue;
                }
                if (Math.Abs(cy.round_nr) > 4 * Math.PI)
                {
                    as2PI form1 = new as2PI(cy, isaxial);
                    form1.Text = cy.identifier + " is more than 2 revolutions, treated as a whole signal";
                    form1.Size = window_size;
                    Application.Run(form1);
                }
                else if (Math.Abs(cy.round_nr) < SC.ZPRange)
                {
                    SegAna form3 = new SegAna(cy, isaxial);
                    form3.Text = cy.identifier + " is only " + Math.Abs(cy.round_nr * 360 / 2 / Math.PI).ToString("F2") + " degree, use Segment Analysis";
                    form3.Size = window_size;
                    Application.Run(form3);
                }
                else if (Math.Abs(cy.round_nr) < 2 * Math.PI)
                {
                    ZP form2 = new ZP(cy, isaxial);
                    form2.Text = cy.identifier + " is almost 1 revolution, zero padding the gap";
                    form2.Size = window_size;
                    Application.Run(form2);
                }
                else
                {
                    CuToOne form4 = new CuToOne(cy, isaxial);
                    form4.Text = cy.identifier + " is more than 1 revolution, use only points in 360 degree for evaluation";
                    form4.Size = window_size;
                    Application.Run(form4);
                }

            }
            SC.sp_f.result_save();
            if (SC.exportPDF)
            {
                if (SC.document.PageCount == 0)
                    return;
                SC.document.Save(SC.outputPath);
                Process.Start(SC.outputPath);
            }
        }
    }
}
