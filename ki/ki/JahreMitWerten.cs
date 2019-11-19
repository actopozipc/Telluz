using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
namespace ki
{
    /// <summary>
    /// Fick auf Dictionary 
    /// Kein Mensch mag Key + Value
    /// Hier also eine eigene Klasse um jeweils ein Jahr und den dazugehörigen Wert einer Kategorie zu speichern
    /// (Vielleicht schreib ich es später auf ein Dictionary um anstatt eine Liste von dieser Klasse zu verwenden)
    /// </summary>
    class YearWithValue
    {
       [LoadColumn(0)]
        public float Year { get; set; } //jahr
      [LoadColumn(1)]
        public Wert Value { get; set; } //wert
   
        public string Name { get; set; } //macht debugging einfach, fick auf arbeitsspeicher
        public int cat_id { get; set; }
        public YearWithValue()
        {

        }
        public YearWithValue(double y, Wert v)
        {
            Year = (float)y;
            Value = v;
        }
        public YearWithValue(double y, Wert v, string n)
        {
            Year = (float)y;
            Value = v;
            Name = n;
        }
        public YearWithValue(double y, Wert v, string n, int c)
        {
            Year = (float)y;
            Value = v;
            Name = n;
            cat_id = c;
        }
    }
}
