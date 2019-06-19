using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class Value
    {
        public decimal eigentlicherwert { get; set; }
        public int mulitplikator { get; set; }
        public Value(decimal e, int m)
        {
            eigentlicherwert = e;
            mulitplikator = m;
        }
    }
}
