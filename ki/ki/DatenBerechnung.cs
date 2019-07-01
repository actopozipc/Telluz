using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kiding;
namespace ki
{
    class CalculateData
    {
        static MySqlConnection connection = null;
        int categorycount;
        const double divident = 10000;
        const int x = 3000;
        public CalculateData()
        {
            string cs = ConnectionString();
            connection = new MySqlConnection(cs);
            connection.Open();
        }
        public List<CategoriesWithYearsAndValues> Generate()
        {
            Countrystats countrystats = new Countrystats(); //Klasse für alle Kategorien und deren Werte per Jahr
            countrystats.country = "Austria"; //Land zu dem die Kategorien mit Werte gehören
            //in der Datenbank sind derzeit nur Werte für Österreich drin
            countrystats.ListWithCategoriesWithYearsAndValues = GetCategoriesWithValuesAndYears(); //Werte mit Jahren
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
                    
                    SingleCategoryData.Add(YearWithValue);
                }
                //Wenn ein Wert nicht dokumentiert ist, ist in der Datenbank 0 drin. Das verfälscht den Wert für die Ki
                //entferne deswegen 0
                SingleCategoryData = RemoveZero(SingleCategoryData);
                //Wenn es mindestens ein Jahr einer Kategorie gibt, in der der Wert nicht 0 ist
                if (SingleCategoryData.Count > 1)
                {

                    //Bearbeite eigenen Datensatz
                    int multi = Scale(SingleCategoryData); //wie viel man die normierten werte mulitplizieren muss damit sie wieder echt sind
                    //Erstelle Task für einzelnen Datensatz um dann auf alle zu warten
                    liste[i] = Task<List<YearWithValue>>.Run(() =>
                        {
                            return Train(SingleCategoryData, 2019, multi);
                        });

                }
                //ohne dieses else gebe es einige leere Tasks im Array -> Exception
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
                inputs.Add(Convert.ToDouble(YearWithValue.year / divident));
                outputs.Add(Convert.ToDouble(YearWithValue.value) / (Math.Pow(10, multi)));
            }
           

            // creating the neurons

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
                    hiddenNeuron1.inputs = inputs[i];
                    outputNeuron.inputs = hiddenNeuron1.output;
                    outputNeuron.error = sigmoid.derived(outputNeuron.output) * (outputs[i] - outputNeuron.output);
                    outputNeuron.adjustWeights();
                    hiddenNeuron1.error = sigmoid.derived(hiddenNeuron1.output) * outputNeuron.error * outputNeuron.weights;
                    hiddenNeuron1.adjustWeights();
                }

                lernvorgang++;
            }

            //bekomme immer das höchste jahr
            double j = KnownValues.Max(i => i.year);

            //wenn das höchste bekannte jahr kleiner ist als das Jahr, bis zu dem wir die Werte wissen wollen, 
            //dann füge das nächste Jahr als input ins Neuron, bekomme den Output und füge es in die Liste mit allen Werten ein
            //Dann Rekursion bis das größte Jahr nicht mehr kleiner ist als das Jahr bis zu dem wir rechnen wollen
            if (j < FutureYear)
            {
                hiddenNeuron1.inputs = inputs[inputs.Count - 1]+ 1 / divident;
                outputNeuron.inputs = hiddenNeuron1.output;
                KnownValues.Add(new YearWithValue(((inputs[inputs.Count-1]+ 1 / divident) * divident), Convert.ToDecimal(outputNeuron.output * Convert.ToDouble(Math.Pow(10,multi)))));
                return Train(KnownValues, FutureYear, multi);
            }
            //wenn alle Jahre bekannt sind, returne die Liste
            else
            {
                return KnownValues;
            }

        }
        int Scale(List<YearWithValue> n)
        {

            double temp = Convert.ToDouble(n.Max(i => i.value));
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
            var temp = collection.Where(i => i.value != 0).ToList();
            return temp;
        }
        public List<CategoriesWithYearsAndValues> GetCategoriesWithValuesAndYears()
        {
            MySqlCommand command = connection.CreateCommand();
            List<string> vs = GetItems();
            List<CategoriesWithYearsAndValues> keyValuePairs = new List<CategoriesWithYearsAndValues>();
            foreach (var item in vs)
            {
                CategoriesWithYearsAndValues kmjw = new CategoriesWithYearsAndValues();
                kmjw.category = item;
                command.CommandText = $"SELECT year, value FROM Daten WHERE item= '{item}';";
                using (MySqlDataReader reader = command.ExecuteReader())
                {

                    List<YearWithValue> temp = new List<YearWithValue>();
                    while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        int tempy = (int)reader["year"];
                        decimal tempv = (decimal)reader["value"];
                        temp.Add(new YearWithValue(tempy, tempv, item));
                    }
                    kmjw.YearsWithValues = temp;
                    keyValuePairs.Add(kmjw);
                }
            }
            return keyValuePairs;

        }
        public List<string> GetItems()
        {
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT item FROM Daten GROUP BY item;";
            List<string> disziplin = new List<string>();
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                {
                    disziplin.Add((string)reader["item"]);
                }
            }
            return disziplin;
        }
        //wird noch schöner und sicherer gemacht
        static string ConnectionString()
        {
            StringBuilder sb = new StringBuilder();
            string server = "localhost";
            string database = "Welt";
            string uid = "root";
            string password = "";
            return sb.Append("SERVER=" + server + ";" + "DATABASE=" +
              database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";").ToString();
        }
    }
}
