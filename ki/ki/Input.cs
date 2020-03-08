using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
namespace ki
{
    /// <summary>
    /// Original-Wert ist Key
    /// Normierter Wert ist Value
    /// </summary>
    class Input
    {
      
        public string name { get; set; }
        private Dictionary<double, double> jahrMitNormierung = new Dictionary<double, double>(); //Year, normiertes jahr
        private Dictionary<double, double> wertMitNormierung = new Dictionary<double, double>();
        public double step { get; set; }
        public void AddJahr(double jahr, double normiert)
        {
            jahrMitNormierung.Add(jahr, normiert);
        }
        public void SetYearsWithNorm(Dictionary<double, double> jahrMitNormierung)
        {
            this.jahrMitNormierung = jahrMitNormierung;
        }
        public void SetValuesWithNorm(Dictionary<double, double> wertMitNormierung)
        {
            this.wertMitNormierung = wertMitNormierung;
        }
        public Dictionary<double, double> GetYearsDic()
        {
            return jahrMitNormierung;
        }
        public Dictionary<double, double> GetValuesDic()
        {
            return wertMitNormierung;
        }
        public void AddWert(double wert, double normiert)
        {
            wertMitNormierung.Add(wert, normiert);
        }
        public double GetNormYear(double jahr)
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
