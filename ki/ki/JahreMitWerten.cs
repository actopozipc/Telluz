using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class JahrMitWert
    {
        public double year { get; set; }
        public decimal value { get; set; }
        public JahrMitWert()
        {

        }
        public JahrMitWert(double y, decimal v)
        {
            year = y;
            value = v;
        }
    }
}
