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

namespace FFTFormLib
{
    public partial class as2PI : template
    {
        public as2PI()
        {
            InitializeComponent();
        }
        public as2PI(Cylinder pp, bool isaxial = false)
            : base(pp, isaxial)
        {
        }
        protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            rn.Clear();
            //get the origin data from special program
            if (_is_axial)
            {
                rn.Add(0);
                for (int i = 1; i < size; ++i)
                {
                    double temp_z = cy.transferedPoints[i].z - cy.transferedPoints[0].z;
                    double delta_z = (cy.angle_commu[i] / 2 / Math.PI) * _lead;
                    rn.Add(temp_z - delta_z);
                }
            }
            else
            {
                for (int i = 0; i < size; ++i)
                {
                    rn.Add(hypot(cy.transferedPoints[i].y, cy.transferedPoints[i].x) - cy.radius);
                }
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
