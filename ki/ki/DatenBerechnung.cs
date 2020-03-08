using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace ki
{
    class CalculateData
    {
        const double divident = 10000;
   
        static DB dB = null;
        CNTK cNTK = new CNTK(dB);

        int categorycount;
        public CalculateData()
        {
            try
            {
                dB = new DB();
            }
            catch (Exception)
            {

                Console.WriteLine("Connection to DB failed");
            }
        }
        public async Task<List<Countrystats>> GenerateForEachCountryAsync(List<int> laenderIDs, List<int> kategorienIDs, int from, int futureYear)
        {
            List<string> laender = await dB.GetCountriesToCategoriesAsync(laenderIDs); //Liste mit allen Ländern
            List<Countrystats> countrystats = new List<Countrystats>();
            for (int i = 0; i < laender.Count; i++)
            {
                await Task.Run(async () =>
                 {
                     Countrystats c = new Countrystats();
                     c.Country = new Country(laender[i]);
                     c.ListWithCategoriesWithYearsAndValues = await GenerateAsync(laender[i], kategorienIDs, from, futureYear);
                     countrystats.Add(c);

                 });

                Console.WriteLine("Land {0} wurde berechnet", laender[i]);
            }
            return countrystats;
        }

        private int DifferentValuesCount(List<YearWithValue> values)
        {
            return values.Distinct().Count();
        }

        private async Task<List<CategoriesWithYearsAndValues>> GenerateAsync(string country, List<int> kategorienIDs, int from, int futureYear)
        {

            Countrystats countrystats = new Countrystats(); //Klasse für alle Kategorien und deren Werte per Jahr
            countrystats.Country = new Country(country); //Land zu dem die Kategorien mit Werte gehören
            countrystats.ListWithCategoriesWithYearsAndValues = await dB.GetCategoriesWithValuesAndYearsAsync(country, kategorienIDs); //Werte mit Jahren
            categorycount = countrystats.ListWithCategoriesWithYearsAndValues.Count; //wie viele kategorien an daten für dieses land existieren
            List<CategoriesWithYearsAndValues> CategorysWithFutureValues = new List<CategoriesWithYearsAndValues>();
            Task<List<YearWithValue>>[] liste = new Task<List<YearWithValue>>[categorycount]; //liste damit jede kategorie in einem task abgearbeitet werden kann
            List<YearWithValue> PopulationTotal = new List<YearWithValue>();

            //Arbeite jede Kategorie parallel ab
            for (int i = 0; i < categorycount; i++)
            {
                //Erstelle für jede Kategorie einen Liste mit eigenen Datensätzen
                List<YearWithValue> SingleCategoryData = new List<YearWithValue>();

                //Hole einzelne Datensätze für jedes Jahr heraus
                foreach (var YearWithValue in countrystats.ListWithCategoriesWithYearsAndValues[i].YearsWithValues)
                {
                    SingleCategoryData.Add(new YearWithValue(YearWithValue.Year, new Wert(Convert.ToDecimal(YearWithValue.Value.value)), countrystats.Country.name, YearWithValue.cat_id));
                }
                //Wenn ein Wert nicht dokumentiert ist, ist in der Datenbank 0 drin. Das verfälscht den Wert für die Ki
                //entferne deswegen 0
                SingleCategoryData = RemoveZero(SingleCategoryData);
                //Wenn es mindestens ein Jahr einer Kategorie gibt, in der der Wert nicht 0 ist
                if (SingleCategoryData.Count > 1)
                {
                    int coaid = await dB.GetCountryByNameAsync(country); //numeric of country
                    int categ = await dB.GetCategoryByNameAsync(countrystats.ListWithCategoriesWithYearsAndValues[i].category.name); //numeric of category
                    //Bearbeite eigenen Datensatz
                    int multi = Scale(SingleCategoryData) - 1; //wie viel man die normierten werte mulitplizieren muss damit sie wieder echt sind

                    if (SingleCategoryData.Any(x => x.cat_id == 4))
                    {
                        PopulationTotal = SingleCategoryData;
                    }
                    if (DifferentValuesCount(SingleCategoryData) > 2)
                    {
                        //linear train
                        liste[i] = Task.Run(async () =>
                       {
                           if (await dB.GetMaxYearAsync(coaid, categ) >= futureYear) //if all wanted values are already known
                           {
                               return SingleCategoryData;
                           }
                           else
                           {
                               if ((SingleCategoryData.Any(x => x.cat_id > 38 && x.cat_id < 46)) || SingleCategoryData.Any(x => x.cat_id == 77)) //if categoy is an emission-type or temp
                               {
                                   ML mL = new ML(dB);
                                   if (dB.CheckModel(coaid, categ)) //check for model
                                   {
                                      
                                       List<YearWithValue> yearWithValues = new List<YearWithValue>();
                                       if (categ == 77) //if temp
                                       {
                                           Model model = dB.LoadModel(0, 77);
                                          yearWithValues = await mL.PredictTempOverYearsAsync(model, futureYear, SingleCategoryData, countrystats.Country);
                                           return yearWithValues;
                                       }
                                       else
                                       {
                                           Model modelContainer = dB.LoadModel(coaid, categ);
                                           yearWithValues = await mL.PredictCo2OverYearsAsync(modelContainer, futureYear, coaid, SingleCategoryData, cNTK);
                                       }
                                       return yearWithValues;

                                   }
                                   else //calculate model
                                   {
                                       if (categ == 77) //if temp
                                       {
                                           Model model = await mL.TrainTempModelAsync(countrystats.Country);
                                           List<YearWithValue> x = await mL.PredictTempOverYearsAsync(model, futureYear, SingleCategoryData, countrystats.Country);
                                           return x;
                                       }
                                       else
                                       {
                                           List<YearWithValue> x = await mL.TrainAndPredictEmissionsAsync(SingleCategoryData, PopulationTotal, futureYear);
                                           return x;
                                       }

                                   }


                               }
                               else //if category is non-emission and no temp
                               {

                                   bool parameterExists = await dB.CheckParametersAsync(coaid, categ); //check if parameter for this country and this category exist
                                   if (parameterExists)
                                   {
                                       Console.WriteLine("Daten werden von Datenbank genommen");
                                       ParameterStorage parStor = await dB.GetParameterAsync(coaid, categ); //Bekomme Parameter
                                       List<YearWithValue> yearWithValues = new List<YearWithValue>();
                                       foreach (var item in countrystats.ListWithCategoriesWithYearsAndValues[i - 1].YearsWithValues)
                                       {
                                           yearWithValues.Add(new YearWithValue(item.Year, new Wert(Convert.ToDecimal(item.Value.value)), countrystats.Country.name, item.cat_id));
                                       }
                                       yearWithValues = RemoveZero(yearWithValues);
                                       yearWithValues = cNTK.Predict(yearWithValues, from, futureYear, parStor);
                                       return yearWithValues;

                                   }
                                   else
                                   {
                                       CNTK cNTK = new CNTK(dB);
                                       List<YearWithValue> x = await cNTK.TrainLinearOneOutputAsync(SingleCategoryData, futureYear);
                                       if (SingleCategoryData.Any(a => a.cat_id == 4))
                                       {

                                           PopulationTotal = x;
                                       }
                                       return PopulationTotal;
                                   }
                               }
                           }
                        

                           // 
                       });
                    }
                    else
                    {
                        liste[i] = Task<List<YearWithValue>>.Run(() =>
                        {
                            return sigmoid.TrainSigmoid(SingleCategoryData, futureYear, multi);
                        });
                    }
                }


                //ohne dieses else gäbe es einige leere Tasks im Array -> Exception
                //ohne if geht die KI datensätze ohne einträge durch -> Verschwendung von Rechenleistung und Zeit
                else
                {
                    liste[i] = Task.Run(() => { return new List<YearWithValue>(); });
                }
            }

            //Warte parallel bis alle Kategorien gelernt und berechnet wurden
            Task.WaitAll(liste);
            //returne alle Kategorien 
            for (int i = 0; i < categorycount; i++)
            {
                CategorysWithFutureValues.Add(new CategoriesWithYearsAndValues(countrystats.ListWithCategoriesWithYearsAndValues[i].category, liste[i].Result));
            }

            return CategorysWithFutureValues;


        }
        //Findet die letzten n höchsten Werte, also zB n= 5 in einem Array mit 10 Zahlen gibt die Zahlen von 5-10 zurück
        List<YearWithValue> RemoveZero(List<YearWithValue> collection)
        {
            var temp = collection.Where(i => i.Value.value != 0).ToList();
            return temp;
        }

        int Scale(List<YearWithValue> n)
        {
            double temp = Convert.ToDouble(n.Max(i => i.Value.value));
            int m = 1;
            while (1 <= temp)
            {
                temp = temp / 10;
                m++;
            }
            return m;
        }

    }
}
