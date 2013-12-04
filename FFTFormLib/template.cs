using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp;
using SPInterface;
using System.Diagnostics;
using System.IO;

namespace FFTFormLib
{
    public partial class template : Form
    {
        protected System.ComponentModel.BackgroundWorker backgroundWorker1;
        protected System.Windows.Forms.ProgressBar progressBar1;
        protected List<double> fft;
        protected double scale_fft_y;
        protected Cylinder cy;
        protected int index;
        protected int size;
        protected List<double> tolerance;
        protected double _lead;
        protected bool _is_axial;
        protected List<double> rn = new List<double>();
        
        public double lead
        {
            get
            {
                return _lead;
            }
            set
            {
                _lead = value;
                textBox_lead.Text = value.ToString();
            }
        }
        protected double hypot(double p1, double p2)
        {
            return Math.Sqrt(p1 * p1 + p2 * p2);
        }
        public template()
        {
            InitializeComponent();

        }
        public template(Cylinder pp, bool isaxial = false)
        {
            InitializeComponent();
            _is_axial = isaxial;
            this.FormClosing += template_FormClosing;
            cy = pp;

            if (!isaxial)
            {
                textBox_lead.Hide();
            }
            else
            {
                textBox_lead.Show();
                DataRow[] result_rows;
                string lead_id = SC.sp_f.sys_dict["planid"];
                lead_id +="_";
                lead_id += cy.identifier;
                string filter = "lead_id = '"+lead_id+"'";
                result_rows = SC.ds.Tables["lead"].Select(filter);
                if (result_rows.Length > 0)
                {
                    lead = Convert.ToDouble(result_rows[0]["lead_value"]);
                }
                else
                {
                    //no used value;
                    //calculate now
                    List<double> z_value = new List<double>();
                    foreach (MeasPoint p in cy.transferedPoints)
                    {
                        z_value.Add(p.z-cy.transferedPoints[0].z);
                    }
                    lead = Math.Round(z_value.Average()*(cy.round_nr/Math.PI) *4,0)/4.0;
                }
                
            }
            size = cy.size;

            index = 0;
            fft = new List<double>();
            tolerance = new List<double>();

            dataGridView1[1, 0].Value = 0.00;
            dataGridView1[2, 0].Value = 0.00;
            //try to get parameter file
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory;// System.IO.Directory.GetCurrentDirectory();
                string name = cy.identifier + ".txt";
                string tol_file_name = System.IO.Path.Combine(path, name);
                try
                {
                    StreamReader sc = new StreamReader(tol_file_name);
                    string mode = sc.ReadLine();
                    if (mode == "1")
                    {
                        SC.R = Convert.ToDouble(sc.ReadLine());
                        SC.n0 = Convert.ToDouble(sc.ReadLine());
                        SC.k = Convert.ToDouble(sc.ReadLine());
                        comboBox1.SelectedIndex = 1;
                        textBox1.Text = SC.R.ToString();
                        textBox2.Text = SC.n0.ToString();
                        textBox3.Text = SC.k.ToString();
                    }
                    else if (mode == "0")
                    {
                        comboBox1.SelectedIndex = 0;
                        List<List<double>> paras = new List<List<double>>();
                        string line;
                        while ((line = sc.ReadLine()) != null)
                        {

                            string[] sg = line.Split(' ');
                            List<double> temp = new List<double>();
                            temp.Add(Convert.ToDouble(sg[0]));
                            temp.Add(Convert.ToDouble(sg[1]));
                            temp.Add(Convert.ToDouble(sg[2]));

                            paras.Add(temp);
                        }
                        while (dataGridView1.Rows.Count <= paras.Count)
                            dataGridView1.Rows.Add();
                        for (int i = 0; i < paras.Count; ++i)
                        {
                            dataGridView1[0, i].Value = paras[i][0];
                            dataGridView1[1, i].Value = paras[i][1];
                            dataGridView1[2, i].Value = paras[i][2];

                        }
                    }

                }
                catch
                {
                    comboBox1.SelectedIndex = 1;
                    textBox1.Text = SC.R.ToString();
                    textBox2.Text = SC.n0.ToString();
                    textBox3.Text = SC.k.ToString();
                }
                update_tolerance();
            }
            backgroundWorker1.RunWorkerAsync();

        }
        protected void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Hide();
            this.Invalidate();
        }

        protected void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            panel2_Paint(this, new PaintEventArgs(panel2.CreateGraphics(), panel2.Bounds));
        }

        virtual protected void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        { }




        protected void template_FormClosing(object sender, FormClosingEventArgs e)
        {
            // generate PDF
            #region PDF
            {


                // Create an empty page
                PdfPage page = SC.document.AddPage();
                page.Size = PageSize.A4;
                //MessageBox.Show(page.Height.ToString(),page.Width.ToString());
                page.Rotate = 90;
                // Get an XGraphics object for drawing
                XGraphics painter = XGraphics.FromPdfPage(page);

                //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always);
                Bitmap pic = new Bitmap(1000, 700);

                Graphics g = Graphics.FromImage(pic);
                panel2_Paint(this, new PaintEventArgs(g, new Rectangle(new Point(0, 0), pic.Size)));
                //panel2.DrawToBitmap(pic, panel2.Bounds);
                painter.DrawImage(pic, 60, 25);

            }
            #endregion

            #region save result to calypso
            //save result to Calypso
            List<KeyValuePair<int, double>> deviation = new List<KeyValuePair<int, double>>();
            for (int i = 2; i < 300; ++i)
            {
                deviation.Add(new KeyValuePair<int, double>(i, tolerance[i - 2] - fft[i - 1] * 1000));
            }
            deviation.Sort((x, y) => (x.Value.CompareTo(y.Value)));
            for (int i = 0; i < SC.no_result; ++i)
            {
                SC.sp_f.addresult(cy.identifier, cy.geoType, deviation[i].Key.ToString(), deviation[i].Value, "", 0, tolerance[i]);
            }
            #endregion

            #region save paras
            //save paras
            string path = System.AppDomain.CurrentDomain.BaseDirectory;// System.IO.Directory.GetCurrentDirectory();
            string name = cy.identifier + ".txt";
            string tol_file_name = System.IO.Path.Combine(path, name);
            StreamWriter sw = new StreamWriter(tol_file_name);
            sw.WriteLine(comboBox1.SelectedIndex.ToString());
            if (comboBox1.SelectedIndex == 0)
            {
                for (int i = 0; i < dataGridView1.Rows.Count - 1; ++i)
                {
                    sw.WriteLine(dataGridView1[0, i].Value.ToString() + " " + dataGridView1[1, i].Value.ToString() + " " + dataGridView1[2, i].Value.ToString());
                }
            }
            else
            {
                sw.WriteLine(textBox1.Text);
                sw.WriteLine(textBox2.Text);
                sw.WriteLine(textBox3.Text);
            }
            sw.Close();

            //for lead
            try
            {
                double result;
                if (double.TryParse(textBox_lead.Text, out result))
                {
                    lead = result;
                }
            }
            catch
            {
            }
            if (_is_axial)
            {
                DataRow[] result_rows;
                string lead_id = SC.sp_f.sys_dict["planid"];
                lead_id += "_";
                lead_id += cy.identifier;
                string filter = "lead_id = '" + lead_id + "'";
                result_rows = SC.ds.Tables["lead"].Select(filter);
                if (result_rows.Length > 0)
                {
                    result_rows[0]["lead_value"] = _lead;
                }
                else
                {
                    DataRow dr = SC.ds.Tables["lead"].NewRow();
                    //no used value;
                    //calculate now
                    dr["lead_id"] = lead_id;
                    dr["lead_value"] = _lead;
                    SC.ds.Tables["lead"].Rows.Add(dr);
                }
                string filename = System.IO.Path.Combine(SC.filepath, "lead.xml");
                SC.ds.WriteXml(filename);
            }

            #endregion
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

            if (index == 0)
                return;
            Graphics painter = e.Graphics;
            painter.Clear(SystemColors.Control);
            painter.ScaleTransform(e.ClipRectangle.Width / 1000.0f, e.ClipRectangle.Height / 700.0f);
            Pen solid_pen = new Pen(Color.Black, 1.3F);
            Pen fft_pen = new Pen(Color.Green, 1.5F);
            Pen tol_pen = new Pen(Color.Blue, 1.0F);
            Pen over_tol_pen = new Pen(Color.Red, 1.8F);
            //painter for radial
            // the drawing area is 900*350
            painter.TranslateTransform(50, 350);

            painter.DrawLine(solid_pen, 0, 0, 900, 0);
            painter.DrawLine(solid_pen, 0, 0, 0, -300);
            Font string_font = new Font("Airal", 10);
            SolidBrush string_brush = new SolidBrush(Color.Black);
            painter.DrawString("0", string_font, string_brush, 0, 2);
            for (int i = 20; i < 300; i += 20)
            {
                painter.DrawString(i.ToString(), string_font, string_brush, i * 3, 2);
            }
            for (int i = 1; i < 6; ++i)
            {
                Rectangle rec = new Rectangle(-45, -i * 50 - 5, 42, 20);
                painter.DrawString((fft.Max() * i * 200).ToString("G3"), string_font, string_brush, rec);
                painter.DrawLine(solid_pen, -3, -i * 50, 0, -i * 50);
            }
            scale_fft_y = 250 / fft.Max();
            Trace.WriteLine(fft.Count.ToString());
            Trace.WriteLine(scale_fft_y.ToString());
            for (int i = 1; i < fft.Count; ++i)
            {
                Pen temp_pen;
                if (i > 1)
                {
                    if (fft[i - 1] < tolerance[i - 2] / 1000.0F)
                        temp_pen = fft_pen;
                    else
                        temp_pen = over_tol_pen;
                }
                else
                {
                    temp_pen = fft_pen;
                }
                painter.DrawLine(temp_pen, i * 3, 0, i * 3, (float)(-fft[i - 1] * scale_fft_y));
            }

            if (index > 298)
            {
                for (int i = 2; i < fft.Count; ++i)
                    painter.DrawLine(tol_pen, i * 3, (float)(-tolerance[i - 2] * 0.001 * scale_fft_y) - 0.5F, (i + 1) * 3, (float)(-tolerance[i - 1] * 0.001 * scale_fft_y));
            }
            painter.TranslateTransform(0, 20);
            int Row_Nr = 20;
            int Col_Nr = 5;
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Far;
            drawFormat.LineAlignment = StringAlignment.Center;
            for (int i = 0; i < Col_Nr; i++)
                for (int j = 0; j < Row_Nr; j++)
                {
                    Rectangle rec = new Rectangle(i * 200 - 10, 20 + j * 15, 35, 15);
                    painter.DrawString((i * Row_Nr + j + 1).ToString("G3"), string_font, string_brush, rec, drawFormat);
                }
            drawFormat.Alignment = StringAlignment.Near;
            drawFormat.LineAlignment = StringAlignment.Center;
            int text_size = Math.Min(100, fft.Count);
            for (int i = 0; i < text_size; ++i)
            {
                Rectangle rec = new Rectangle(30 + (i / Row_Nr) * 200, 20 + (i % Row_Nr) * 15, 100, 15);
                painter.DrawString((fft[i] * 1000).ToString("f4"), string_font, string_brush, rec, drawFormat);
            }

        }
        private void update_tolerance()
        {
            try
            {
                if (comboBox1.SelectedIndex == 1)
                {
                    //RTA Curve
                    double r, n0, k;
                    r = Convert.ToDouble(textBox1.Text);
                    n0 = Convert.ToDouble(textBox2.Text);
                    k = Convert.ToDouble(textBox3.Text);

                    tolerance.Clear();

                    for (int i = 2; i < 310; ++i)
                    {

                        double v = r / (Math.Pow(i - 1, n0 + k / i));
                        tolerance.Add(v);
                    }
                }
                else
                {
                    int size = dataGridView1.Rows.Count - 1;
                    List<List<double>> paras = new List<List<double>>();
                    for (int i = 0; i < size; i++)
                    {
                        List<double> temp = new List<double>();
                        temp.Add(Convert.ToDouble(dataGridView1[0, i].Value));
                        temp.Add(Convert.ToDouble(dataGridView1[1, i].Value));
                        temp.Add(Convert.ToDouble(dataGridView1[2, i].Value));
                        paras.Add(temp);
                    }
                    tolerance.Clear();
                    for (int i = 2; i < 310; ++i)
                    {

                        double v = 0;
                        for (int j = 0; j < size; ++j)
                        {
                            v += paras[j][1] * Math.Pow((i + paras[j][2]), paras[j][0]);
                        }
                        tolerance.Add(v);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please check the input of the tolerance");
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            update_tolerance();
            if (_is_axial)
            {
                backgroundWorker1.RunWorkerAsync();
            }

            panel2_Paint(this, new PaintEventArgs(panel2.CreateGraphics(), panel2.Bounds));

        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex > 0)
            {
                if (dataGridView1[e.ColumnIndex, e.RowIndex].Value == "")
                {
                    dataGridView1.Rows.Remove(dataGridView1.Rows[e.RowIndex]);
                }
            }
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            dataGridView1[1, e.RowIndex].Value = 0.0;
            dataGridView1[2, e.RowIndex].Value = 0.0;

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (comboBox1.SelectedIndex == 1)
            {
                //RTA
                panel3.Hide();
                panel4.Show();
            }
            else
            {
                //polynomial
                panel4.Hide();
                panel3.Show();
            }
            button1.PerformClick();
        }

        private void textBox_lead_Leave(object sender, EventArgs e)
        {
            double result;
            if (!double.TryParse(textBox_lead.Text, out result))
            {
                MessageBox.Show("Please input a number");
                textBox_lead.Focus();
            }
            else
            {
                _lead = result;
            }
        }


    }
}

