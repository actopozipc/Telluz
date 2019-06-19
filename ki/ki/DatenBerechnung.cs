using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class DatenBerechnung
    {
        static MySqlConnection conn = null;
        int kategoriencount;
        const double jm = 10000;
        public DatenBerechnung()
        {
            string cs = ConnectionString();
            conn = new MySqlConnection(cs);
            conn.Open();
        }
        public void OnGet()
        {

        }
        public List<KategorieMitJahrenUndWerten> GeneriereVorhersage()
        {
            Landesstatistik l = new Landesstatistik(); //Klasse für alle Kategorien und deren Werte per Jahr
            l.country = "Austria"; //Land zu dem die Kategorien mit Werte gehören
            l.ListeMitKategorienMitJahrenUndWerten = GetCategoriesWithValuesAndYears(); //Werte mit Jahren
            kategoriencount = l.ListeMitKategorienMitJahrenUndWerten.Count;
            List<KategorieMitJahrenUndWerten> KategorienMitZukünftigenWerten = new List<KategorieMitJahrenUndWerten>();

            //Arbeite jede Kategorie parallel ab
            Parallel.For(0, kategoriencount, i =>
            {

                    //Erstelle für jede Kategorie einen Liste mit eigenen Datensätzen
                    List<JahrMitWert> einzelnerdatensatz = new List<JahrMitWert>();

                    //Hole einzelne Datensätze für jedes Jahr heraus
                    foreach (var JahrMitWert in l.ListeMitKategorienMitJahrenUndWerten[i].JahreMitWerten)
                {
                    einzelnerdatensatz.Add(JahrMitWert);
                }
                    //Wenn ein Wert nicht dokumentiert ist, ist in der Datenbank 0 drin. Das verfälscht den Wert für die Ki
                    einzelnerdatensatz = EntferneNull(einzelnerdatensatz);
                    //Wenn es mindestens ein Jahr einer Kategorie gibt, in der der Wert nicht 0 ist
                    if (einzelnerdatensatz.Count > 1)
                {

                        //Bearbeite eigenen Datensatz

                        int multi = WertNormieren(einzelnerdatensatz);
                    lock (KategorienMitZukünftigenWerten)
                    {
                        KategorienMitZukünftigenWerten.Add(new KategorieMitJahrenUndWerten(l.ListeMitKategorienMitJahrenUndWerten[i].description, Trainieren(einzelnerdatensatz, 2019, multi)));
                    }


                }
            });

            return KategorienMitZukünftigenWerten;

        }
        //Sieht anhand einer Liste mit Jahren und Zahlen den Wert für Jahr Ziel voraus
        private List<JahrMitWert> Trainieren(List<JahrMitWert> d, int ZukunftsJahr, int multi)
        {
            List<double> inputs = new List<double>();
            List<double> outputs = new List<double>();

            foreach (var item in d)
            {
                inputs.Add(Convert.ToDouble(item.year / jm));
                outputs.Add(Convert.ToDouble(item.value) / (Math.Pow(10, multi)));
            }
         
           
        // creating the neurons
    
            Neuron hiddenNeuron1 = new Neuron();
            Neuron outputNeuron = new Neuron();
            hiddenNeuron1.randomizeWeights();
            outputNeuron.randomizeWeights();
   
            List<JahrMitWert> zukunft = new List<JahrMitWert>();
            for (int i = 0; i < outputs.Count; i++)
            {
                zukunft.Add(new JahrMitWert(Convert.ToDouble(inputs[i]), Convert.ToDecimal(outputs[i])));
            }
            int lernvorgang = 0;
            int z = inputs.Count;
            while (lernvorgang < 3000)
            {

                for (int i = 0; i <z; i++)
                {
                    hiddenNeuron1.inputs = new double[] { inputs[i] };
                    outputNeuron.inputs = new double[] { hiddenNeuron1.output };
                    outputNeuron.error = sigmoid.abgeleitet(outputNeuron.output) * (Convert.ToDouble(outputs[i]) - outputNeuron.output);
                    outputNeuron.adjustWeights();
                    hiddenNeuron1.error = sigmoid.abgeleitet(hiddenNeuron1.output) * outputNeuron.error * outputNeuron.weights[0];
                    hiddenNeuron1.adjustWeights();
                }
                lernvorgang++;

            }
            //bekomme immer das höchste jahr
            double j = Convert.ToDouble(inputs[inputs.Count - 1]) * jm;


            if (j < ZukunftsJahr)
            {
                    double i = ++j / jm;
                    inputs.Add(i);
                    hiddenNeuron1.inputs = new double[] {Convert.ToDouble( i)};
                    outputNeuron.inputs = new double[] { hiddenNeuron1.output };
                    outputs.Add(outputNeuron.output);
                    zukunft.Add(new JahrMitWert(i, Convert.ToDecimal(outputNeuron.output)));
            }
            //Normiere Werte
            for (int i = 0; i < zukunft.Count; i++)
            {
                zukunft[i].value = Convert.ToDecimal(zukunft[i].value * Convert.ToDecimal(Math.Pow(10, multi)));
                zukunft[i].year = zukunft[i].year * jm;
            }
            if (j<ZukunftsJahr)
            {
                return Trainieren(zukunft, ZukunftsJahr, multi);
            }
            else
            {
                return zukunft;
            }
          
    

        }
        int WertNormieren(List<JahrMitWert> n)
        {
            var groessterwert = n[0].value;
            for (int i = 0; i < n.Count; i++)
            {
                if (n[i].value > groessterwert)
                {
                    groessterwert = n[i].value;
                }
            }

            double temp = Convert.ToDouble(groessterwert);
            int m = 1;
            while (1 <= temp)
            {
                temp = temp / 10;
                m++;
            }

            return m;
        }
        decimal WertAbsurdisieren(double n, int m)
        {
            return Convert.ToDecimal(n * (10 * m));
        }
        List<JahrMitWert> EntferneNull(List<JahrMitWert> collection)
        {
            List<JahrMitWert> temp = new List<JahrMitWert>();
            foreach (var item in collection)
            {
                if (item.value != 0)
                {
                    temp.Add(item);
                }
            }

            return temp;
        }
        public List<KategorieMitJahrenUndWerten> GetCategoriesWithValuesAndYears()
        {
            MySqlCommand command = conn.CreateCommand();
            List<string> vs = GetItems();
            List<KategorieMitJahrenUndWerten> keyValuePairs = new List<KategorieMitJahrenUndWerten>();
            foreach (var item in vs)
            {
                KategorieMitJahrenUndWerten kmjw = new KategorieMitJahrenUndWerten();
                kmjw.description = item;
                command.CommandText = $"SELECT year, value FROM Daten WHERE item= '{item}';";
                using (MySqlDataReader reader = command.ExecuteReader())
                {

                    List<JahrMitWert> temp = new List<JahrMitWert>();
                    while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        int tempy = (int)reader["year"];
                        decimal tempv = (decimal)reader["value"];
                        temp.Add(new JahrMitWert(tempy, tempv));


                    }
                    kmjw.JahreMitWerten = temp;
                    keyValuePairs.Add(kmjw);
                }
            }
            return keyValuePairs;

        }
        public List<string> GetItems()
        {
            MySqlCommand command = conn.CreateCommand();
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
