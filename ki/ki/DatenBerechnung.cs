using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ki
{
    class CalculateData
    {
        static NpgsqlConnection connection = null;
        int categorycount;
        const double divident = 10000;
        const int x = 210;
        public CalculateData()
        {
            string cs = ConfigurationManager.ConnectionStrings["klimaki"].ConnectionString;
            connection = new NpgsqlConnection(cs);
            connection.Open();
        }
        public async Task<List<Countrystats>> GenerateForEachCountryAsync(List<int> laenderIDs, List<int> kategorienIDs)
        {
            List<string> laender = GetCountries(laenderIDs); //Liste mit allen Ländern
            List<Countrystats> countrystats = new List<Countrystats>();
            for (int i = 0; i < laender.Count; i++)
            {
                await Task.Run(async () =>
                 {
                     Countrystats c = new Countrystats();
                     c.Country = laender[i];
                     c.ListWithCategoriesWithYearsAndValues = await GenerateAsync(laender[i], kategorienIDs);
                     countrystats.Add(c);

                 });

                Console.WriteLine("Land {0} wurde berechnet", laender[i]);
            }
            return countrystats;
        }
        public async Task<List<CategoriesWithYearsAndValues>> GenerateAsync(string country, List<int> kategorienIDs)
        {

            Countrystats countrystats = new Countrystats(); //Klasse für alle Kategorien und deren Werte per Jahr
            countrystats.Country = country; //Land zu dem die Kategorien mit Werte gehören
            //in der Datenbank sind derzeit nur Werte für Österreich drin
            countrystats.ListWithCategoriesWithYearsAndValues = await GetCategoriesWithValuesAndYearsAsync(country, kategorienIDs); //Werte mit Jahren
            categorycount = countrystats.ListWithCategoriesWithYearsAndValues.Count; //wie viele kategorien an daten für dieses land existieren
            List<CategoriesWithYearsAndValues> CategorysWithFutureValues = new List<CategoriesWithYearsAndValues>();
            Task<List<YearWithValue>>[] liste = new Task<List<YearWithValue>>[categorycount]; //liste damit jede kategorie in einem task abgearbeitet werden kann

            //Arbeite jede Kategorie parallel ab
            for (int i = 0; i < categorycount; i++)
            {

                //Erstelle für jede Kategorie einen Liste mit eigenen Datensätzen
                List<YearWithValue> SingleCategoryData = new List<YearWithValue>();

                //Hole einzelne Datensätze für jedes Jahr heraus
                foreach (var YearWithValue in countrystats.ListWithCategoriesWithYearsAndValues[i].YearsWithValues)
                {

                    SingleCategoryData.Add(new YearWithValue(YearWithValue.Year, YearWithValue.Value, countrystats.ListWithCategoriesWithYearsAndValues[i].category));
                }
                //Wenn ein Wert nicht dokumentiert ist, ist in der Datenbank 0 drin. Das verfälscht den Wert für die Ki
                //entferne deswegen 0
                SingleCategoryData = RemoveZero(SingleCategoryData);
                //Wenn es mindestens ein Jahr einer Kategorie gibt, in der der Wert nicht 0 ist
                if (SingleCategoryData.Count > 1)
                {

                    //Bearbeite eigenen Datensatz
                    int multi = Scale(SingleCategoryData) - 1; //wie viel man die normierten werte mulitplizieren muss damit sie wieder echt sind
                    //Erstelle Task für einzelnen Datensatz um dann auf alle zu warten
                    liste[i] = Task<List<YearWithValue>>.Run(() =>
                       {
                           return Train(SingleCategoryData, 2030, multi);
                       });

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
        /// <summary>
        /// Sieht anhand einer Liste mit Jahren und Zahlen den Wert für Jahr Ziel voraus
        /// </summary>
        /// <param name="KnownValues">Liste bekannter Werte anhand deren die KI lernt</param>
        /// <param name="FutureYear"> Bis zu welchem Jahr die KI werte vorhersagen soll</param>
        /// <param name="multi">Wie viel man normierte Werte mulitplizieren muss</param>
        /// <returns>Liste mit allen bereits bekannten Werten + Vorhersagen für zukünftige Werte</returns>
        private List<YearWithValue> Train(List<YearWithValue> KnownValues, int FutureYear, int multi)
        {

            List<double> inputs = new List<double>(); //Jahre
            List<double> outputs = new List<double>(); //Werte 
            //Für jedes Jahr mit Wert in der Liste mit Jahren und Werten d
            foreach (var YearWithValue in KnownValues)
            {
                inputs.Add(Convert.ToDouble(YearWithValue.Year));
                outputs.Add(Convert.ToDouble(YearWithValue.Value) / (Math.Pow(10, multi)));
            }
            Input input = Standardization(inputs, FutureYear);
            Neuron hiddenNeuron1 = new Neuron();
            Neuron outputNeuron = new Neuron();
            hiddenNeuron1.randomizeWeights();
            outputNeuron.randomizeWeights();


            int lernvorgang = 0;
            int z = KnownValues.Count;
            //Trainiere alle bekannten Werte x mal
            while (lernvorgang < x)
            {

                for (int i = 0; i < z; i++)
                {
                    hiddenNeuron1.inputs = input.getNormierterWert(inputs[i]);
                    outputNeuron.inputs = hiddenNeuron1.output;
                    outputNeuron.error = sigmoid.derived(outputNeuron.output) * (outputs[i] - outputNeuron.output);
                    outputNeuron.adjustWeights();
                    hiddenNeuron1.error = sigmoid.derived(hiddenNeuron1.output) * outputNeuron.error * outputNeuron.weights;
                    hiddenNeuron1.adjustWeights();

                }

                lernvorgang++;
            }

            //bekomme immer das höchste jahr
            double j = KnownValues.Max(i => i.Year);

            //wenn das höchste bekannte jahr kleiner ist als das Jahr, bis zu dem wir die Werte wissen wollen, 
            //dann füge das nächste Jahr als input ins Neuron, bekomme den Output und füge es in die Liste mit allen Werten ein
            //Dann Rekursion bis das größte Jahr nicht mehr kleiner ist als das Jahr bis zu dem wir rechnen wollen
            if (j < FutureYear)
            {

                hiddenNeuron1.inputs = input.getNormierterWert(j) + input.step;
                outputNeuron.inputs = hiddenNeuron1.output;
                KnownValues.Add(new YearWithValue((Math.Round((inputs[inputs.Count - 1] + 1))), Convert.ToDecimal(outputNeuron.output * Convert.ToDouble(Math.Pow(10, multi)))));
                return Train(KnownValues, FutureYear, multi);
            }
            //wenn alle Jahre bekannt sind, returne die Liste
            else
            {
                return KnownValues;
            }

        }
        Input Standardization(List<double> inputs, int Zukunftsjahr)
        {
            Input input = new Input();
            inputs = inputs.Distinct().ToList(); //Ich weiß dass ich ein Hashset verwenden könnte, aber ich weiß nicht ob sich das von der Performance lohnt. Add in Hashset = braucht länger als liste, dafür konsumiert liste.distinct zeit

            double maxvalue = inputs.Max();
            double count = inputs.Count;
            double diff = Zukunftsjahr - maxvalue;
            double step = 1 / (count + diff);
            List<double> normierteWerte = new List<double>();
            input.step = step;
            double i = 0;

            foreach (var item in inputs)
            {
                input.Add(item, i);
                i = i + step;
            }

            return input;
        }
        int Scale(List<YearWithValue> n)
        {

            double temp = Convert.ToDouble(n.Max(i => i.Value));
            int m = 1;
            while (1 <= temp)
            {
                temp = temp / 10;
                m++;
            }

            return m;
        }
        List<YearWithValue> RemoveZero(List<YearWithValue> collection)
        {
            var temp = collection.Where(i => i.Value != 0).ToList();
            return temp;
        }
        public async Task<List<CategoriesWithYearsAndValues>> GetCategoriesWithValuesAndYearsAsync(string country, List<int> kategorieIDs)
        {
            NpgsqlCommand command = connection.CreateCommand();
            List<string> vs = GetItems(kategorieIDs);
            List<CategoriesWithYearsAndValues> keyValuePairs = new List<CategoriesWithYearsAndValues>();
            foreach (var item in vs)
            {

                CategoriesWithYearsAndValues kmjw = new CategoriesWithYearsAndValues
                {
                    category = item
                };
                command.CommandText = $"SELECT year, ROUND(values, 15) FROM input_data JOIN category c on input_data.cat_id = c.cat_id JOIN country_or_area coa on input_data.coa_id = coa.coa_id WHERE c.name = '{item}' AND coa.name = '{country}';";
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {

                    List<YearWithValue> temp = new List<YearWithValue>();
                    while (await reader.ReadAsync()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        int tempy = (int)reader["year"];
                        decimal tempv = (decimal)reader["round"];
                        temp.Add(new YearWithValue(tempy, tempv, item));
                    }
                    kmjw.YearsWithValues = temp;
                    keyValuePairs.Add(kmjw);
                }

            }
            return keyValuePairs;

        }
        public List<string> GetItems(List<int> catids)
        {
            NpgsqlCommand command = connection.CreateCommand();
            if (catids.Count == 0)
            {
                command.CommandText = "SELECT name FROM category ORDER BY category;";
            }
            else
            {
                List<string> statementConditions = new List<string>();
                foreach (var item in catids)
                {
                    statementConditions.Add("cat_id=" + item);
                }
                command.CommandText = ConcatSQLConditions(statementConditions, "SELECT name FROM category") + " ORDER BY category;";
            }

            List<string> disziplin = new List<string>();
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                {
                    disziplin.Add((string)reader["name"]);
                }
            }
            return disziplin;
        }
        public List<string> GetCountries(List<int> coaids)
        {
            NpgsqlCommand command = connection.CreateCommand();
            if (coaids.Count == 0)
            {
                command.CommandText = "SELECT name FROM country_or_area;";
            }
            else
            {
                List<string> statementConditions = new List<string>();

                foreach (var item in coaids)
                {
                    statementConditions.Add("coa_id=" + item);
                }

                command.CommandText = ConcatSQLConditions(statementConditions, "SELECT name FROM country_or_area") + ";";

            }

            List<string> land = new List<string>();
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                {
                    land.Add((string)reader["name"]);
                }
            }
            return land;
        }
        private string ConcatSQLConditions(List<string> ids, string statement)
        {
            string temp = String.Join(" OR ", ids);
            return statement + " WHERE " + temp;
        }
    }
}
