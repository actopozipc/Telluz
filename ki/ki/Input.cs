using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
namespace ki
{
    class Input
    {
        public Dictionary<float, float> input = new Dictionary<float, float>();
       // public Output output;
        private Dictionary<double, double> jahrMitNormierung = new Dictionary<double, double>();
        private Dictionary<double, double> wertMitNormierung = new Dictionary<double, double>();
        public double step { get; set; }
        public void AddJahr(double jahr, double normiert)
        {
            jahrMitNormierung.Add(jahr, normiert);
        }
        public void AddWert(double wert, double normiert)
        {
            wertMitNormierung.Add(wert, normiert);
        }
        public double getNormierterWert(double jahr)
        {
           return jahrMitNormierung[jahr];
        }
        public float[] getAlleJahreNormiert()
        {
            float[] array = new float[jahrMitNormierung.Count];
           
            for (int i = 0; i < jahrMitNormierung.Count; i++)
            {
                array[i] = float.Parse(jahrMitNormierung.ElementAt(i).Value.ToString());
            }
            return array;
        }
        public float[] getAlleWerteNormiert()
        {
            float[] werte = new float[wertMitNormierung.Count];
            for (int i = 0; i < wertMitNormierung.Count; i++)
            {
                werte[i] = float.Parse(wertMitNormierung.ElementAt(i).Value.ToString());
            }
            return werte;
        }
        public Dictionary<double,double> GetPairs()
        {
            return jahrMitNormierung;
        }
    }
}
