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
            double temp = 1 / (1.0 + Math.Exp(-x));

            return temp;

        }

        public static double derived(double x)
        {
            return output(x) * (1 - output(x));
        }
    }
}
