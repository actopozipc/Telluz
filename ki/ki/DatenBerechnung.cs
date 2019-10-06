using CNTK;
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
       static DB dB = null;
        int categorycount;
        const double divident = 10000;
        const int x = 210;
        public CalculateData()
        {
            string cs = ConfigurationManager.ConnectionStrings["klimaki"].ConnectionString;
            dB = new DB(cs);
        }
        public async Task<List<Countrystats>> GenerateForEachCountryAsync(List<int> laenderIDs, List<int> kategorienIDs)
        {
            List<string> laender = dB.GetCountries(laenderIDs); //Liste mit allen Ländern
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
            countrystats.ListWithCategoriesWithYearsAndValues = await dB.GetCategoriesWithValuesAndYearsAsync(country, kategorienIDs); //Werte mit Jahren
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
                  
                           if (DifferentValuesCount(SingleCategoryData)>2)
                           {
                        //linear train
                        TrainLinear(SingleCategoryData, 2040);

                           }
                           else
                           {
                        liste[i] = Task<List<YearWithValue>>.Run(() =>
                        {
                            return TrainSigmoid(SingleCategoryData, 2030, multi);
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
        /// <summary>
        /// Sieht anhand einer Liste mit Jahren und Zahlen den Wert für Jahr Ziel voraus
        /// </summary>
        /// <param name="KnownValues">Liste bekannter Werte anhand deren die KI lernt</param>
        /// <param name="FutureYear"> Bis zu welchem Jahr die KI werte vorhersagen soll</param>
        /// <param name="multi">Wie viel man normierte Werte mulitplizieren muss</param>
        /// <returns>Liste mit allen bereits bekannten Werten + Vorhersagen für zukünftige Werte</returns>
        private List<YearWithValue> TrainSigmoid(List<YearWithValue> KnownValues, int FutureYear, int multi)
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
                return TrainSigmoid(KnownValues, FutureYear, multi);
            }
            //wenn alle Jahre bekannt sind, returne die Liste
            else
            {
                return KnownValues;
            }

        }
        private List<YearWithValue> TrainLinear(List<YearWithValue> KnownValues, int FutureYear)
        {
            var device = DeviceDescriptor.UseDefaultDevice();
            //Step 2: define values, and variables
            Variable x = Variable.InputVariable(new int[] { 1 }, DataType.Float, "input");
            Variable y = Variable.InputVariable(new int[] { 1 }, DataType.Float, "output");
            //Step 2: define training data set from table above
            float[] inputs = CategoriesWithYearsAndValues.GetYearsFromList(KnownValues);
         
            float[] outputs = CategoriesWithYearsAndValues.GetValuesFromList(KnownValues);
            var xValues = Value.CreateBatch(new NDShape(1, 1), inputs, device);
            var yValues = Value.CreateBatch(new NDShape(1, 1), outputs, device);
            //Step 3: create linear regression model
            var lr = createLRModel(x, device);
            //Network model contains only two parameters b and w, so we query
            //the model in order to get parameter values
            var paramValues = lr.Inputs.Where(z => z.IsParameter).ToList();
            var totalParameters = paramValues.Sum(c => c.Shape.TotalSize);
            //Step 4: create trainer
            var trainer = createTrainer(lr, y);
            //Ştep 5: training
            double b = 0, w = 0;
            for (int i = 1; i <= 200; i++)
            {
                var d = new Dictionary<Variable, Value>();
                d.Add(x, xValues);
                d.Add(y, yValues);
                //
                trainer.TrainMinibatch(d, true, device);
                //
                var loss = trainer.PreviousMinibatchLossAverage();
                var eval = trainer.PreviousMinibatchLossAverage();
                //
                if (i % 20 == 0)
                    Console.WriteLine($"It={i}, Loss={loss}, Eval={eval}");

                if (i == 200)
                {
                    //print weights
                    var b0_name = paramValues[0].Name;
                    var b0 = new Value(paramValues[0].GetValue()).GetDenseData<float>(paramValues[0]);
                    var b1_name = paramValues[1].Name;
                    var b1 = new Value(paramValues[1].GetValue()).GetDenseData<float>(paramValues[1]);
                    Console.WriteLine($" ");
                    Console.WriteLine($"Training process finished with the following regression parameters:");
                    Console.WriteLine($"b={b0[0][0]}, w={b1[0][0]}");
                    b = b0[0][0];
                    w = b1[0][0];
                    Console.WriteLine($" ");
                }
            }
            double j = KnownValues.Max(i => i.Year);
            if (j<FutureYear)
            {
                for (double i = j+1; i < FutureYear; i++)
                {
                    KnownValues.Add(new YearWithValue(i, (Convert.ToDecimal(w*i+b))));
                }
            }
            return KnownValues;
        }
        public Trainer createTrainer(Function network, Variable target)
        {
            //learning rate
            var lrate = 0.082;
            var lr = new TrainingParameterScheduleDouble(lrate);
            //network parameters
            var zParams = new ParameterVector(network.Parameters().ToList());

            //create loss and eval
            Function loss = CNTKLib.SquaredError(network, target);
            Function eval = CNTKLib.SquaredError(network, target);

            //learners
            //
            var llr = new List<Learner>();
            var msgd = Learner.SGDLearner(network.Parameters(), lr);
            llr.Add(msgd);

            //trainer
            var trainer = Trainer.CreateTrainer(network, loss, eval, llr);
            //
            return trainer;
        }
        private  Function createLRModel(Variable x, DeviceDescriptor device)
        {
            //initializer for parameters
            var initV = CNTKLib.GlorotUniformInitializer(1.0, 1, 0, 1);

            //bias
            var b = new Parameter(new NDShape(1, 1), DataType.Float, initV, device, "b"); ;

            //weights
            var W = new Parameter(new NDShape(2, 1), DataType.Float, initV, device, "w");

            //matrix product
            var Wx = CNTKLib.Times(W, x, "wx");

            //layer
            var l = CNTKLib.Plus(b, Wx, "wx_b");

            return l;
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
        private int DifferentValuesCount(List<YearWithValue> values)
        {
            return values.Distinct().Count();
        }
       
    }
}
