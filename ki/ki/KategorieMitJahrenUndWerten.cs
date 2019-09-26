using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class CategoriesWithYearsAndValues
    {
        public string category { get; set; } //name der kategorie
        public List<YearWithValue> YearsWithValues { get; set; } //Liste mit Jahr + Value
        public CategoriesWithYearsAndValues(string category, List<YearWithValue> YearsWithValues)
        {
            this.category = category;
            this.YearsWithValues = YearsWithValues;
        }
        public CategoriesWithYearsAndValues(string category, Task<List<YearWithValue>> YearsWithValuesTask)
        {
            this.category = category;
            YearsWithValues = YearsWithValuesTask.Result;
        }
     public   static List<double> GetYearsFromList(List<YearWithValue> list)
        {
            List<double> jahre = new List<double>();
            foreach (var item in list)
            {
                jahre.Add(item.Year);
            }
            return jahre;
        }
        public static List<decimal> GetValuesFromList(List<YearWithValue> list)
        {
            List<decimal> werte = new List<decimal>();
            foreach (var item in list)
            {
                werte.Add(item.Value);
            }
         
            return werte;
        }
        //Konstruktor nicht löschen
        public CategoriesWithYearsAndValues()
        {

        }
    }
}
