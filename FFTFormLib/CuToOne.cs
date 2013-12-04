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
    public partial class CuToOne : template
    {
        public CuToOne()
        {
            InitializeComponent();
        }
        public CuToOne(Cylinder pp, bool isaxial = false)
            : base(pp, isaxial)
        {
        }
        protected override void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            double step = Math.Abs(cy.round_nr) / size;
            int point_nr = Convert.ToInt32(Math.Floor(2 * Math.PI / step));
            rn.Clear();
            //calculate the Radial by DFT
            // This is the correct & origin definition
            // for all frequency, this algrothm is slow, but it's much faster when only calc little frequency

            //get the origin data from special program
            if (_is_axial)
            {
                rn.Add(0);
                for (int i = 1; i < point_nr; ++i)
                {
                    double temp_z = cy.transferedPoints[i + (size - point_nr) / 2].z - cy.transferedPoints[(size - point_nr) / 2].z;
                    double delta_z = ((cy.angle_commu[i + (size - point_nr) / 2] - cy.angle_commu[(size - point_nr) / 2]) / 2 / Math.PI) * _lead;
                    rn.Add(temp_z - delta_z);
                }
            }
            else
            {
                for (int i = 0; i < point_nr; ++i)
                {
                    rn.Add(hypot(cy.transferedPoints[i + (size - point_nr) / 2].y, cy.transferedPoints[i + (size - point_nr) / 2].x) - cy.radius);
                }
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
