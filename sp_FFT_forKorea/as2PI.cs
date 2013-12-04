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

namespace sp_FFT_forKorea
{
    public partial class as2PI : template
    {
        public as2PI()
        {
            InitializeComponent();
        }
        public as2PI(Cylinder pp)
            : base(pp)
        {

        }
        protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<double> rn = new List<double>();
            //get the origin data from special program
            for (int i = 0; i < size; ++i)
            {
                rn.Add(hypot(cy.transferedPoints[i].y, cy.transferedPoints[i].x) - cy.radius);
            }


            double re, im;
            re = 0;
            im = 0;

            double divi = size / (2 * 3.1415926);
            for (int j = 1; j < 300; j++)
            {
                for (int i = 0; i < size; ++i)
                {
                    re += (double)((Math.Sin((i / divi) * j) * rn[i]) / size); // 'realanteil integration
                    im += (double)(Math.Cos((i / divi) * j) * rn[i]) / size; // 'imagin盲ranteil
                }
                fft.Add(2 * hypot(re, im)); //            'Amplitude berechen
                re = 0;
                im = 0;
            }
            index = 300;
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(100);
            //((BackgroundWorker)sender).ReportProgress(100);
        }
    }
}
