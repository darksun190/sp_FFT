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
    public partial class CuToOne : template
    {
        public CuToOne()
        {
            InitializeComponent();
        }
        public CuToOne(Cylinder pp)
            : base(pp)
        {
        }
        protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            double step = Math.Abs(cy.round_nr) / size;
            int point_nr = Convert.ToInt32(Math.Floor(2 * Math.PI / step));

            //calculate the Radial by DFT
            // This is the correct & origin definition
            // for all frequency, this algrothm is slow, but it's much faster when only calc little frequency

            List<double> rn = new List<double>();
            //get the origin data from special program
            for (int i = 0; i < point_nr; ++i)
            {
                rn.Add(hypot(cy.transferedPoints[i + (size - point_nr) / 2].y, cy.transferedPoints[i + (size - point_nr) / 2].x) - cy.radius);
            }

            double re, im;
            re = 0;
            im = 0;

            double divi = point_nr / (2 * 3.1415926);
            fft = new List<double>();
            for (int j = 1; j < 300; j++)
            {
                for (int i = 0; i < point_nr; ++i)
                {
                    re += (double)((Math.Sin((i / divi) * j) * rn[i]) / point_nr); // 'realanteil integration
                    im += (double)(Math.Cos((i / divi) * j) * rn[i]) / point_nr; // 'imagin盲ranteil
                }
                fft.Add(2 * hypot(re, im)); //            'Amplitude berechen
                re = 0;
                im = 0;
            }
            index = fft.Count;
            ((BackgroundWorker)sender).ReportProgress(100);
        }
    }
}
