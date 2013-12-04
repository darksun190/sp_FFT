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
    public partial class ZP : template
    {
        public ZP()
        {
            InitializeComponent();
        }
        public ZP(Cylinder pp)
            : base(pp)
        {
        }
       protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int padding_nr = Convert.ToInt32(Math.Floor(
                (2 * Math.PI - Math.Abs(cy.round_nr))
                / (cy.round_nr / cy.size)));
            int size = cy.size;
            //calculate the Radial by DFT
            // This is the correct & origin definition
            // for all frequency, this algrothm is slow, but it's much faster when only calc little frequency

            List<double> rn = new List<double>();
            //get the origin data from special program
            for (int i = 0; i < size; ++i)
            {
                rn.Add(hypot(cy.transferedPoints[i].y, cy.transferedPoints[i].x) - cy.radius);
            }
            for (int i = 0; i < padding_nr; ++i)
            {
                rn.Add(0.0);
            }
            size += padding_nr;

            double re, im;
            re = 0;
            im = 0;

            double divi = size / (2 * 3.1415926);
            fft = new List<double>();
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
            ((BackgroundWorker)sender).ReportProgress(100);
        }
     }
}
