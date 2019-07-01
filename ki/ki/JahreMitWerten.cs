using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public double year { get; set; } //jahr
        public decimal value { get; set; } //wert
        public string name { get; set; } //macht debugging einfach, fick auf arbeitsspeicher
        public YearWithValue()
        {

        }
        public YearWithValue(double y, decimal v)
        {
            year = y;
            value = v;
        }
        public YearWithValue(double y, decimal v, string n)
        {
            year = y;
            value = v;
            name = n;
        }
    }
}
