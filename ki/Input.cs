using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class Input
    {
        private Dictionary<double, double> jahrMitNormierung = new Dictionary<double, double>();
        public double step { get; set; }
        public void Add(double jahr, double normiert)
        {
            jahrMitNormierung.Add(jahr, normiert);
        }
        public double getNormierterWert(double jahr)
        {
           return jahrMitNormierung[jahr];
        }
    }
}
