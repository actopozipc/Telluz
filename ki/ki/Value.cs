using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class Wert
    {
        public float value { get; private set; }
       public bool berechnet { get; private set; }
        public Wert(float value)
        {
            this.value = value;
            berechnet = false;
        }
        public Wert(float value, bool berechnet)
        {
            this.value = value;
            this.berechnet = berechnet;
        }
        //Macht vieles einfacher
        public Wert(decimal v)
        {
            this.value = (float)v;

        }
        public Wert(decimal v, bool b)
        {
            this.value = (float)v;
            this.berechnet = b;

        }
    }
}
