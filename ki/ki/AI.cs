using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class AI
    {
        public static List<YearWithValue> RemoveZero(List<YearWithValue> collection)
        {
            var temp = collection.Where(i => i.Value.value != 0).ToList();
            return temp;
        }

        public DB dB = null;
        public AI(DB dB)
        {
            this.dB = dB;
        }
        public static bool CompareBiggestValueToFutureYear(List<YearWithValue> yearWithValues, int futureYear)
        {
            return (yearWithValues.Max(v => v.Year) < futureYear);
        }

        /// <summary>
        /// Calculates future values based on alreadyknown Parameters
        /// </summary>
        /// <param name="yearWithValues">List that gets extended</param>
        /// <param name="from">startyear</param>
        /// <param name="futureYear">year in the future</param>
        /// <param name="parStor">parameter</param>
        /// <returns></returns>
        public List<YearWithValue> Predict(List<YearWithValue> yearWithValues, int from, int futureYear, ParameterStorage parStor)
        {
            double j = yearWithValues.Max(k => k.Year);
            float valueToDiv = CategoriesWithYearsAndValues.GetValuesFromList(yearWithValues).Max();
            float[] inputs = CategoriesWithYearsAndValues.GetYearsFromList(yearWithValues);
            List<double> listForTheNormalizedInputs = new List<double>();
            foreach (var item in inputs)
            {
                listForTheNormalizedInputs.Add(item); //Small brain schleife?
            }
            Input input = StandardizationYears(listForTheNormalizedInputs, futureYear);
            inputs = input.getAlleJahreNormiert();
            if (j < futureYear)
            {
                float inputsMax = inputs.Max();
                while (j < futureYear)
                {
                    j++;
                    yearWithValues.Add(new YearWithValue(j, new Wert((parStor.W * inputsMax + parStor.b) * valueToDiv)));
                    float[] inputtemp = CategoriesWithYearsAndValues.GetYearsFromList(yearWithValues);
                    List<double> fuckinghelpme = new List<double>();
                    foreach (var item in inputtemp)
                    {
                        fuckinghelpme.Add(item); //Small brain schleife?
                    }
                    Input input2 = StandardizationYears(fuckinghelpme, futureYear);
                    inputtemp = input2.getAlleJahreNormiert();
                    inputsMax = inputtemp.Max();
                }
            }
            else //cut list from year to futureyear
            {
                if (futureYear > from)
                {
                    int indexMax = yearWithValues.FindIndex(a => a.Year == Convert.ToInt32(futureYear)); //finde Index von Jahr bis zu dem man Daten braucht
                    yearWithValues.RemoveRange(indexMax, yearWithValues.Count - indexMax); //Cutte List von Jahr bis zu dem man es braucht bis Ende

                    int indexMin = yearWithValues.FindIndex(b => b.Year == Convert.ToInt32(from));
                    yearWithValues.RemoveRange(0, indexMin);
                }
                else
                {
                    var temp = yearWithValues.Where(x => x.Year == from);
                    yearWithValues = temp.ToList(); ;
                }
            }
            return yearWithValues;
        }

     public   static Input StandardizationYears(List<double> inputs, int Zukunftsjahr)
        {
            Input input = new Input();
            inputs = inputs.Distinct().ToList(); //Ich weiß dass ich ein Hashset verwenden könnte, aber ich weiß nicht ob sich das von der Performance lohnt. Add in Hashset = braucht länger als liste, dafür konsumiert liste.distinct zeit
            double maxvalue = inputs.Max();
            double count = inputs.Count;
            double diff = Zukunftsjahr - maxvalue;
            if (diff < 0)
            {
                diff = diff * 2;
            }
            double step = 1 / (count + diff);
            List<double> normierteWerte = new List<double>();
            input.step = step;
            double i = 0;
            foreach (var item in inputs)
            {
                input.AddJahr(item, i);
                i = i + step;
            }
            return input;
        }
     public   static Input StandardizationValues(List<double> inputs, int Zukunftsjahr)
        {
            Input input = new Input();
            double min = inputs.Min();
            double max = inputs.Max();
            foreach (var value in inputs)
            {
                input.AddWert(value, (value - min) / (max - min));
            }
            return input;
        }
     public   static Input Standarization(List<YearWithValue> inputs, int Zukunftsjahr)
        {
            Input input = new Input();
            List<double> years = new List<double>();
            List<double> values = new List<double>();
            foreach (var item in inputs)
            {
                years.Add(item.Year);
                values.Add(item.Value.value);
            }
            Input inputYears = StandardizationYears(years, Zukunftsjahr);
            Input inputValues = StandardizationValues(values, Zukunftsjahr);
            input.SetYearsWithNorm(inputYears.GetYearsDic());
            input.SetValuesWithNorm(inputValues.GetValuesDic());
            return input;
        }
        public List<YearWithValue> GetDistinctValues(List<YearWithValue> yearWithValues)
        {
            for (int i = 0; i < yearWithValues.Count; i++)
            {
                for (int j = 1; j < yearWithValues.Count - 1; j++)
                {
                    if (yearWithValues[i].Value.value == yearWithValues[j].Value.value)
                    {
                        yearWithValues.Remove(yearWithValues[j]);
                    }
                }
            }
            return yearWithValues;
        }
        public float[] GetLastNValues(float[] array, int n, double step)
        {
            int count = array.Count();
            int temp = n;
            float[] f = new float[n];
            for (int i = count - 1; i > count - n; i--)
            {
                f[temp - 1] = array[i];
                temp--;
            }
            f[0] = float.Parse(Convert.ToString(f[1] - step));
            return f;
        }
    }
}
