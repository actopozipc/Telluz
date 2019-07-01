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
        //Konstruktor nicht löschen
        public CategoriesWithYearsAndValues()
        {

        }
    }
}
