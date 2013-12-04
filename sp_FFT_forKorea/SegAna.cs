using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SPInterface;
using System.Diagnostics;

namespace sp_FFT_forKorea
{
    public partial class SegAna : template
    {
        public SegAna()
        {
            InitializeComponent();
        }
        public SegAna(Cylinder pp) : base(pp)
        {

        }
        protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Stopwatch ti = new Stopwatch();
            ti.Start();
            BackgroundWorker worker = sender as BackgroundWorker;

            List<double> rn = new List<double>();
            //get the origin data from special program
            for (int i = 0; i < size; ++i)
            {
                rn.Add(hypot(cy.transferedPoints[i].y, cy.transferedPoints[i].x) - cy.radius);
            }
            double re, im;
            re = 0;
            im = 0;
            List<double> temp_fft = new List<double>();
            double step = Math.Abs(cy.round_nr) / cy.size;
            fft.Add(0.0);       //for 1upr 
            int loop_nr = 0;
            {
                for (index = 2; index < 300; index++)
                {
                    //the j is the upr of the fft
                    //calculate the evaluation range
                    temp_fft.Clear();

                    if (index % 20 == 0)
                        worker.ReportProgress(index / 3);

                    double div_j = 2 * Math.PI / Convert.ToDouble(index);
                    int Nr_j = 1;
                    if (index > 6)
                    {
                        Nr_j = Convert.ToInt32(Math.Floor(Math.Abs(cy.round_nr) / div_j - 1));

                    }
                    int sub_size;
                    sub_size = Convert.ToInt32(Math.Floor(Nr_j * div_j / step));
                    double divi = sub_size / (2 * Math.PI);
                    if (index == 2)
                    {
                        loop_nr = size - sub_size - 1;
                    }
                    else
                    {
                        loop_nr = Convert.ToInt32(Math.Floor(div_j / step));
                    }
                    for (int k = 0; k < loop_nr; ++k)
                    {
                        for (int i = 0; i < sub_size; ++i)
                        {
                            re += (double)((Math.Sin((i / divi) * (Nr_j)) * rn[k + i]) / sub_size); // 'realanteil integration
                            im += (double)(Math.Cos((i / divi) * (Nr_j)) * rn[k + i]) / sub_size; // 'imagin盲ranteil
                        }
                        temp_fft.Add(2 * hypot(re, im)); //            'Amplitude berechen
                        re = 0;
                        im = 0;


                    }
                    //Console.WriteLine(temp_fft.Min());
                    fft.Add(temp_fft.Min());
                }
                //qDebug()<<"point number is: " << size;

            }


            ti.Stop();
            Console.WriteLine(ti.ElapsedMilliseconds.ToString());
        }
    }
}
