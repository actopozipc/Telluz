using CNTK;

using Microsoft.ML;
using Microsoft.ML.Trainers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
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
            List<YearWithValue> PopulationTotal = new List<YearWithValue>();
            //Arbeite jede Kategorie parallel ab
            for (int i = 0; i < categorycount; i++)
            {

                //Erstelle für jede Kategorie einen Liste mit eigenen Datensätzen
                List<YearWithValue> SingleCategoryData = new List<YearWithValue>();
          
                //Hole einzelne Datensätze für jedes Jahr heraus
                foreach (var YearWithValue in countrystats.ListWithCategoriesWithYearsAndValues[i].YearsWithValues)
                {
                    SingleCategoryData.Add(new YearWithValue(YearWithValue.Year, Convert.ToDecimal(YearWithValue.Value), countrystats.ListWithCategoriesWithYearsAndValues[i].category, YearWithValue.cat_id));
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
                    if (SingleCategoryData.Any(x => x.cat_id == 4))
                    {
                        PopulationTotal = SingleCategoryData;
                        Console.WriteLine();
                    }
                    if (DifferentValuesCount(SingleCategoryData)>2)
                           {
                        //linear train
                        liste[i] = Task<List<YearWithValue>>.Run(() =>
                        {
                            if (SingleCategoryData.Any(x=>x.cat_id > 38 && x.cat_id <45 ))
                            {
                                return TrainLinearMoreInputsMLNET(SingleCategoryData, PopulationTotal, 2030);
                              
                            }
                            else
                            {
                                
                                return TrainLinearOneOutput(SingleCategoryData, 2030, multi);
                            }
                           
                           // 
                        });
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
        private List<YearWithValue> TrainLinearOneOutput(List<YearWithValue> KnownValues, int FutureYear, int multi)
        {

            var device = DeviceDescriptor.UseDefaultDevice();
            ////Step 2: define values, and variables
            Variable x = Variable.InputVariable(new NDShape(1,1), DataType.Float, "input");
            Variable y = Variable.InputVariable(new NDShape(1, 1), DataType.Float, "output");
            ////Step 2: define training data set from table above
            float[] inputs = CategoriesWithYearsAndValues.GetYearsFromList(KnownValues);
            List<double> temp = new List<double>();
            foreach (var item in inputs)
            {
                temp.Add(item); //Small brain schleife?
            }
            Input input = Standardization(temp, FutureYear);
            inputs = input.getAlleJahreNormiert();
            float[] outputs = CategoriesWithYearsAndValues.GetValuesFromList(KnownValues);
            //Value.CreateBatch(Tensor(Achsen, Dimension), Werte, cpu/gpu)

            var xValues = Value.CreateBatch(new NDShape(1, 1), GetLastNValues(inputs, 20, input.step), device);
            var yValues = Value.CreateBatch(new NDShape(1, 1), GetLastNValues(outputs, 20, input.step), device);
            ////Step 3: create linear regression model
            var lr = createLRModel(x, device);
            ////Network model contains only two parameters b and w, so we query
            ////the model in order to get parameter values
            var paramValues = lr.Inputs.Where(z => z.IsParameter).ToList();
            var totalParameters = paramValues.Sum(c => c.Shape.TotalSize);
            ////Step 4: create trainer
            var trainer = createTrainer(lr, y);
            ////Ştep 5: training
            double b = 0, w = 0;
            int max = 20000;
            for (int i = 1; i <= max; i++)
            {
                var d = new Dictionary<Variable, Value>();
                d.Add(x, xValues);
                d.Add(y, yValues);
                //
                trainer.TrainMinibatch(d, true, device);
                //
                var loss = trainer.PreviousMinibatchLossAverage();
                var eval = trainer.PreviousMinibatchEvaluationAverage();
                //
                if (i % 2000 == 0)
                    Console.WriteLine($"It={i}, Loss={loss}, Eval={eval}");

                if (i == max)
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

            if (j < FutureYear)
            {
                float i = inputs.Max();

                while (j < FutureYear)
                {
                    j++;

                    KnownValues.Add(new YearWithValue(j, (Convert.ToDecimal(w * i + b))));
                    float[] inputtemp = CategoriesWithYearsAndValues.GetYearsFromList(KnownValues);
                    List<double> fuckinghelpme = new List<double>();
                    foreach (var item in inputtemp)
                    {
                        fuckinghelpme.Add(item); //Small brain schleife?
                    }
                    Input input2 = Standardization(fuckinghelpme, FutureYear);
                    inputtemp = input2.getAlleJahreNormiert();
                    i = inputtemp.Max();

                }



            }
            return KnownValues;
        }
        private List<YearWithValue> TrainLinearMoreInputsMLNET(List<YearWithValue> ListWithCO, List<YearWithValue> Population, int FutureYear)
        {
            MLContext mlContext = new MLContext(seed: 0);
            List<TwoInputRegressionModel> inputs = new List<TwoInputRegressionModel>();
            foreach (var JahrMitCO in ListWithCO)
            {
                float tempyear = JahrMitCO.Year;
                foreach (var JahrMitPopulation in Population)
                {
                    if (JahrMitPopulation.Year == tempyear)
                    {
                        inputs.Add(new TwoInputRegressionModel() { Year = tempyear, Population = JahrMitPopulation.Value, Co2 = JahrMitCO.Value });
                    }
                }
            }
            var model = Train(mlContext, inputs);
            TestSinglePrediction(mlContext, model);
            return new List<YearWithValue>();
        }
        
        public static ITransformer Train(MLContext mlContext, List<TwoInputRegressionModel> inputs)
        {
            // <Snippet6>
            IDataView dataView = mlContext.Data.LoadFromEnumerable<TwoInputRegressionModel>(inputs);
            // </Snippet6>

            // <Snippet7>
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "Co2")
                    // </Snippet7>
                    // <Snippet8>

                    // </Snippet8>
                    // <Snippet9>
                    .Append(mlContext.Transforms.Concatenate("Features", "Year", "Population"))
                    // </Snippet9>
                    // <Snippet10>
                    .Append(mlContext.Regression.Trainers.FastTree());
            // </Snippet10>


            Console.WriteLine("=============== Create and Train the Model ===============");

            // <Snippet11>
            var model = pipeline.Fit(dataView);
            // </Snippet11>

            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();
            // <Snippet12>
            return model;
            // </Snippet12>
        }
        public enum Activation
        {
            None,
            ReLU,
            Sigmoid,
            Tanh
        }
        private List<YearWithValue> TrainLinearMoreInputs(List<List<YearWithValue>> ListWithKnownValues, int FutureYear, int multi)
        {
            var device = DeviceDescriptor.UseDefaultDevice();
          

            //Network definition
            //Inputs sind Werte jeder Liste + das Jahr, zB wenn Liste 1 im Jahr 2015 den Wert 3 und Liste 2 den Wert 12 hat, wird der Input 2015, 3 und der Output 12 sein
            int InputsCount = ListWithKnownValues.Count; //anzahl der input parameter
            int OutputsCount = 1; //wie viele outputs raus kommen
            int numHiddenLayers = 1; //anzahl der hidden layer
            int hidenLayerDim = 6; //wie viele "knoten" der hidden layer hat
          

            //load data in to memory
            var dataSet = LoadData(ListWithKnownValues[0], ListWithKnownValues[1], InputsCount, OutputsCount);

            // build a NN model
            //define input and output variable
            var xValues = Value.CreateBatch<float>(new NDShape(1, InputsCount), dataSet.Item1, device);
            var yValues = Value.CreateBatch<float>(new NDShape(1, OutputsCount), dataSet.Item2, device);

            // build a NN model
            //define input and output variable and connecting to the stream configuration
            var feature = Variable.InputVariable(new NDShape(1, InputsCount), DataType.Float);
            var label = Variable.InputVariable(new NDShape(1, OutputsCount), DataType.Float);

            //Combine variables and data in to Dictionary for the training
            var dic = new Dictionary<Variable, Value>();
            dic.Add(feature, xValues);
            dic.Add(label, yValues);

            //Build simple Feed Froward Neural Network model
            // var ffnn_model = CreateMLPClassifier(device, numOutputClasses, hidenLayerDim, feature, classifierName);
            var ffnn_model = createFFNN(feature, numHiddenLayers, hidenLayerDim, OutputsCount, Activation.Tanh, "IrisNNModel", device);

            //Loss and error functions definition
            var trainingLoss = CNTKLib.CrossEntropyWithSoftmax(new Variable(ffnn_model), label, "lossFunction");
            var classError = CNTKLib.ClassificationError(new Variable(ffnn_model), label, "classificationError");

            // set learning rate for the network
            var learningRatePerSample = new TrainingParameterScheduleDouble(0.001125, 1);

            //define learners for the NN model
            var ll = Learner.SGDLearner(ffnn_model.Parameters(), learningRatePerSample);

            //define trainer based on ffnn_model, loss and error functions , and SGD learner
            var trainer = Trainer.CreateTrainer(ffnn_model, trainingLoss, classError, new Learner[] { ll });

            //Preparation for the iterative learning process
            //used 800 epochs/iterations. Batch size will be the same as sample size since the data set is small
            int epochs = 800;
            int i = 0;
            while (epochs > -1)
            {

                trainer.TrainMinibatch(dic, device);

                //print progress
                printTrainingProgress(trainer, i++, 50);

                //
                epochs--;
            }
            //Summary of training
            double acc = Math.Round((1.0 - trainer.PreviousMinibatchEvaluationAverage()) * 100, 2);
           
            Console.WriteLine($"------TRAINING SUMMARY--------");
            Console.WriteLine($"The model trained with the accuracy {acc}%");
            return new List<YearWithValue>();
          //  return KnownValues;
        }
        /// <summary>
        /// Gibt eine Tulpe mit Inputs und Outputs zurück
        /// </summary>
        /// <param name="ListOfListOfYearWithValue">Liste mit Listen von Inputs und Outputs</param>
        /// <param name="InputNumber">Anzahl Inputs</param>
        /// <param name="OutputNumber">Anzahl Outputs</param>
        /// <returns></returns>
        static (float[], float[]) LoadData(List<YearWithValue> InputList, List<YearWithValue> OutputList,  int InputNumber, int OutputNumber)
        {
            var features = new List<float>();
            var label = new List<float>();
            //Überprüfe, ob beide Kategorien gleich viele Einträge haben
            if (InputList.Count != OutputList.Count)
            {
                List<YearWithValue> NewInput;
                List<YearWithValue> NewOutput;

                //Meier
              
                //TODO: Methode schreiben die Listen so cuttet dass beide in den gleichen Jahren einen Eitnrag haben
                if (InputList.Count > OutputList.Count)
                {
                    var result = OutputList.Join(InputList, element1 => element1.Year, element2 => element2.Year, (element1, element2) => element1);
                    //for (int i = 0; i < InputList.Count; i++)
                    //{
                    //    var item = InputList[i];
                    //    if (OutputList.Any(x => x.Year == item.Year))
                    //    {
                    //        continue;
                    //    }
                    //    else
                    //    {
                    //        InputList.Remove(item);
                    //    }
                    //}
                }

                else
                {
                    var result = InputList.Join(OutputList, element1 => element1.Year, element2 => element2.Year, (element1, element2) => element1);
                    //for (int i = 0; i < OutputList.Count; i++)
                    //{
                    //    var item = OutputList[i];
                    //    if (InputList.Any(x => x.Year == item.Year))
                    //    {
                    //        continue;
                    //    }
                    //    else
                    //    {
                    //        OutputList.Remove(item);
                    //    }
                    //}
                }
                LoadData(InputList, OutputList, InputNumber, OutputNumber);
            }
            else
            {
                var length = InputList.Count;
                
                //Für jedes Element in den beiden Listen
                for (int i = 0; i < length; i++)
                {
                    float[] input = new float[InputNumber];
                    for (int j = 0; j < InputNumber-1; j++)
                    {
                        input[j] = InputList[i].Value;
                        input[j + 1] = InputList[i].Year;
                    }
                    float[] output = new float[OutputNumber];
                    for (int k = 0; k < OutputNumber; k++)
                    {
                        output[k] = OutputList[i].Value;
                    }
                    features.AddRange(input);
                    label.AddRange(output);
                }
               
            }
            return (features.ToArray(), label.ToArray());


        }
        private static void printTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();
                Console.WriteLine($"Minibatch: {minibatchIdx} CrossEntropyLoss = {trainLossValue}, EvaluationCriterion = {evaluationValue}");
            }
        }
        private Function createFFNN(Variable input, int hiddenLayerCount, int hiddenDim, int outputDim, Activation activation, string modelName, DeviceDescriptor device)
        {
            //First the parameters initialization must be performed
            var glorotInit = CNTKLib.GlorotUniformInitializer(
                    CNTKLib.DefaultParamInitScale,
                    CNTKLib.SentinelValueForInferParamInitRank,
                    CNTKLib.SentinelValueForInferParamInitRank, 1);

            //hidden layers creation
            //first hidden layer
            Function h = simpleLayer(input, hiddenDim, device);
            h = applyActivationFunction(h, activation);
            for (int i = 1; i < hiddenLayerCount; i++)
            {
                h = simpleLayer(h, hiddenDim, device);
                h = applyActivationFunction(h, activation);
            }
            //the last action is creation of the output layer
            var r = simpleLayer(h, outputDim, device);
            r.SetName(modelName);
            return r;
        }
        private static Function applyActivationFunction(Function layer, Activation actFun)
        {
            switch (actFun)
            {
                default:
                case Activation.None:
                    return layer;
                case Activation.ReLU:
                    return CNTKLib.ReLU(layer);
                case Activation.Sigmoid:
                    return CNTKLib.Sigmoid(layer);
                case Activation.Tanh:
                    return CNTKLib.Tanh(layer);
            }
        }
        private static Function simpleLayer(Function input, int outputDim, DeviceDescriptor device)
        {
            //prepare default parameters values
            var glorotInit = CNTKLib.GlorotUniformInitializer(
                    CNTKLib.DefaultParamInitScale,
                    CNTKLib.SentinelValueForInferParamInitRank,
                    CNTKLib.SentinelValueForInferParamInitRank, 1);

            //
            var var = (Variable)input;
            var shape = new int[] { outputDim, var.Shape[0] };
            var weightParam = new Parameter(shape, DataType.Float, glorotInit, device, "w");
            var biasParam = new Parameter(new NDShape(1, outputDim), 0, device, "b");


            return CNTKLib.Times(weightParam, input) + biasParam;

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
        private static void Evaluate(MLContext mlContext, ITransformer model, List<YearWithValue> list)
        {
            IDataView data = mlContext.Data.LoadFromEnumerable<YearWithValue>(list);
            var predictions = model.Transform(data);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");
            
            Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
            
            Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:#.##}");
        }
        private static void TestSinglePrediction(MLContext mlContext, ITransformer model)
        {
            var predictionFunction = mlContext.Model.CreatePredictionEngine<TwoInputRegressionModel, TwoInputRegressionPrediction>(model);
            var test = new TwoInputRegressionModel() {Year = 2017, Population = 3.75f, Co2 = 0 };
            var prediction = predictionFunction.Predict(test);
            Console.WriteLine(prediction.Co2);
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
        //Findet die letzten n höchsten Werte, also zB n= 5 in einem Array mit 10 Zahlen gibt die Zahlen von 5-10 zurück
        private float[] GetLastNValues(float[] array, int n, double step)
        {
            int count = array.Count();
            int temp = n;
            float[] f = new float[n];
            for (int i = count-1; i >count-n; i--)
            {
                f[temp-1] = array[i];
                temp--;
            }
            f[0] = float.Parse(Convert.ToString(f[1] - step));
            return f;
        }
       
    }
}
