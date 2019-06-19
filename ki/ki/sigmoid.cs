using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class sigmoid
    {
        //https://de.wikipedia.org/wiki/Sigmoidfunktion
        public static double output(double x)
        {
            double temp = 1 / (1 + Math.Exp(-x));

            return temp;

        }

        public static double abgeleitet(double x)
        {
            return x * (1.0 - x);
        }
    }
}
