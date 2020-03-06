using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class Countrystats
    {
        public Country Country { get; set; } //name des landes
        //liste mit beschreibung der kategorie sowie eine liste mit allen jahren und werten enthält
        public List<CategoriesWithYearsAndValues> ListWithCategoriesWithYearsAndValues { get; set; } 

        private bool containsAnyValues { get; set; }

        public bool doesContainAnyValues()
        {
            if (ListWithCategoriesWithYearsAndValues.Count>=1)
            {
                foreach (var item in ListWithCategoriesWithYearsAndValues)
                {
                    if (item.YearsWithValues.Count > 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
