using CNTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class CNTK : AI
    {
        public CNTK (DB dB) : base (dB)
        {
            this.dB = dB;
        }
        public enum Activation
        {
            None,
            ReLU,
            Sigmoid,
            Tanh
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

        /// <summary>
        /// Gibt eine Tulpe mit Inputs und Outputs zurück
        /// </summary>
        /// <param name="ListOfListOfYearWithValue">Liste mit Listen von Inputs und Outputs</param>
        /// <param name="InputNumber">Anzahl Inputs</param>
        /// <param name="OutputNumber">Anzahl Outputs</param>
        /// <returns></returns>
        static (float[], float[]) LoadData(List<YearWithValue> InputList, List<YearWithValue> OutputList, int InputNumber, int OutputNumber)
        {
            var features = new List<float>();
            var label = new List<float>();
            //Überprüfe, ob beide Kategorien gleich viele Einträge haben
            if (InputList.Count != OutputList.Count)
            {


                //Meier

                //TODO: Methode schreiben die Listen so cuttet dass beide in den gleichen Jahren einen Eitnrag haben
                if (InputList.Count > OutputList.Count)
                {
                    var result = OutputList.Join(InputList, element1 => element1.Year, element2 => element2.Year, (element1, element2) => element1);
                }

                else
                {
                    var result = InputList.Join(OutputList, element1 => element1.Year, element2 => element2.Year, (element1, element2) => element1);

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
                    for (int j = 0; j < InputNumber - 1; j++)
                    {
                        input[j] = InputList[i].Value.value;
                        input[j + 1] = InputList[i].Year;
                    }
                    float[] output = new float[OutputNumber];
                    for (int k = 0; k < OutputNumber; k++)
                    {
                        output[k] = OutputList[i].Value.value;
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
        private Function createLRModel(Variable x, DeviceDescriptor device)
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

                trainer.TrainMinibatch(dic, false, device);

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
        public async Task<List<YearWithValue>> TrainLinearOneOutputAsync(List<YearWithValue> KnownValues, int FutureYear)
        {

            var device = DeviceDescriptor.UseDefaultDevice();
            ////Step 2: define values, and variables
            Variable x = Variable.InputVariable(new NDShape(1, 1), DataType.Float, "input");
            Variable y = Variable.InputVariable(new NDShape(1, 1), DataType.Float, "output");
            ////Step 2: define training data set from table above
            float[] inputs = CategoriesWithYearsAndValues.GetYearsFromList(KnownValues);
            List<double> temp = new List<double>();
            foreach (var item in inputs)
            {
                temp.Add(item); //Small brain schleife?
            }
            Input input = StandardizationYears(temp, FutureYear);
            inputs = input.getAlleJahreNormiert();
            float[] outputs = CategoriesWithYearsAndValues.GetValuesFromList(KnownValues);
            //Value.CreateBatch(Tensor(Achsen, Dimension), Werte, cpu/gpu)
            float[] outputsnormiert = new float[outputs.Count()];
            float WertZumDividieren = outputs.Max();
            for (int i = 0; i < outputs.Length; i++)
            {
                outputsnormiert[i] = outputs[i] / WertZumDividieren;
            }
            //Werte normiert lassen, sonst stackoverflow :>
            var xValues = Value.CreateBatch(new NDShape(1, 1), GetLastNValues(inputs, inputs.Length, input.step), device);
            var yValues = Value.CreateBatch(new NDShape(1, 1), GetLastNValues(outputsnormiert, outputs.Length, input.step), device);
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
            int max = 2000;

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
                if (i % 200 == 0)
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
                    ParameterStorage ps = new ParameterStorage(float.Parse(w.ToString()), float.Parse(b.ToString()));
                    int coaid = await dB.GetCountryByNameAsync(KnownValues.Where(k => k.Name != null).First().Name);
                    await dB.SaveParameterAsync(ps, coaid, KnownValues.Where(k => k.cat_id != 0).First().cat_id, loss);
                    Console.WriteLine(KnownValues.Min(k => k.Year));
                    KnownValues = Predict(KnownValues, Convert.ToInt32(KnownValues.Min(k => k.Year)), FutureYear, ps);
                    
                }
            }


            return KnownValues;
        }
        public async Task<List<YearWithValue>> PredictPopulationAsync(int coaid, int futureYear, List<YearWithValue> population)
        {
            int cat_id = 4;
            string name = await dB.GetCountryNameByIDAsync(coaid);
            if (await dB.CheckParametersAsync(coaid, 4))
            {
                Predict(population, Convert.ToInt32(population.Max(x => x.Year)), futureYear, await dB.GetParameterAsync(coaid, cat_id));

            }
            else
            {
                //TODO: calculate parameter
                population[0].cat_id = 4;
                population[0].Name = name;
                return await TrainLinearOneOutputAsync(population, futureYear);

            }
            return population;
        }
    }
}
