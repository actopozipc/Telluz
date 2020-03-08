using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class CategoriesWithYearsAndValues
    {
        public Category category { get; set; } //name der kategorie
        public List<YearWithValue> YearsWithValues { get; set; } //Liste mit Jahr + Value
        public CategoriesWithYearsAndValues(Category category, List<YearWithValue> YearsWithValues)
        {
            this.category = category;
            this.YearsWithValues = YearsWithValues;
            
        }
        public CategoriesWithYearsAndValues(Category category, Task<List<YearWithValue>> YearsWithValuesTask)
        {
            this.category = category;
            YearsWithValues = YearsWithValuesTask.Result;
        }
     public   static float[] GetYearsFromList(List<YearWithValue> list)
        {
            int count = list.Count;
            float[] jahre = new float[count];
            for (int i = 0; i < count; i++)
            {
                jahre[i] = float.Parse(list[i].Year.ToString());
            }
            return jahre;
        }
        public static float[] GetValuesFromList(List<YearWithValue> list)
        {
            int count = list.Count;
            float[] werte = new float[count];
            for (int i = 0; i < count; i++)
            {
                werte[i] = float.Parse(list[i].Value.value.ToString());
            }
            return werte;
        }
        //Konstruktor nicht löschen
        public CategoriesWithYearsAndValues()
        {

        }
    }
}
