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
        public float Value { get; set; } //wert
   
        public string Name { get; set; } //macht debugging einfach, fick auf arbeitsspeicher
        public YearWithValue()
        {

        }
        public YearWithValue(double y, decimal v)
        {
            Year = float.Parse(y.ToString());
            Value = float.Parse(v.ToString());
        }
        public YearWithValue(double y, decimal v, string n)
        {
            Year = float.Parse(y.ToString());
            Value = float.Parse(v.ToString());
            Name = n;
        }
    }
}
