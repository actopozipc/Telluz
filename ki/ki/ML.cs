using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class ML : AI
    {
        public ML(DB dB) : base(dB)
        {
            this.dB = dB;
        }
        public static Model Train(MLContext mlContext, List<EmissionModel> inputs)
        {

            IDataView dataView = mlContext.Data.LoadFromEnumerable<EmissionModel>(inputs);
            IDataView trainData = mlContext.Data.TrainTestSplit(dataView).TrainSet;
            IEstimator<ITransformer> pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "Co2")
                    .Append(mlContext.Transforms.Concatenate("Features", "Year", "Population"))

                    .Append(mlContext.Regression.Trainers.Sdca(maximumNumberOfIterations: 2000));

            Console.WriteLine("1");

            Console.WriteLine("=============== Create and Train the Model ===============");


            ITransformer model = pipeline.Fit(dataView); 
            Console.WriteLine("2");
            Console.WriteLine("=============== End of training ===============");
            
            return new Model(model, mlContext, dataView);
        }
        public static double GetErrorMLNet(Model modelContainer)
        {
            var predictions = modelContainer.trainedModel.Transform(modelContainer.data);
            var metrics = modelContainer.mLContext.Regression.Evaluate(predictions, "Label", "Score");
            return metrics.RSquared;
        }
        public async Task<List<YearWithValue>> PredictCo2OverYearsAsync(Model modelContainer, int futureYear, int coa_id, List<YearWithValue> emissions, CNTK cNTK)
        {
            //Get Population till future year
            List<YearWithValue> population = await dB.GetPopulationByCoaIdAsync(coa_id); //get population that is known 

            if (CompareBiggestValueToFutureYear(population, futureYear))     //check if known population is enough to predict emission
            {
                population = await cNTK.PredictPopulationAsync(coa_id, futureYear, population); //get population to predict emission
            }
            EmissionModel[] populationData = new EmissionModel[population.Count];
            for (int i = 0; i < populationData.Count(); i++)
            {
                populationData[i] = new EmissionModel() { Year = population[i].Year, Population = population[i].Value.value };
            }
            PredictionEngine<EmissionModel, EmissionPrediction> predictionEngine = modelContainer.mLContext.Model.CreatePredictionEngine<EmissionModel, EmissionPrediction>(modelContainer.trainedModel);
            IDataView inputData = modelContainer.mLContext.Data.LoadFromEnumerable(populationData);
            IDataView predictions = modelContainer.trainedModel.Transform(inputData);
            float[] scoreColumn = predictions.GetColumn<float>("Score").ToArray();

            for (int i = emissions.Count; i < scoreColumn.Length; i++)
            {
                emissions.Add(new YearWithValue(population[i].Year, new Wert(scoreColumn[i], true)));
            }
            return emissions;

        }
        public static YearWithValue PredictCo2(MLContext mlContext, ITransformer model, float year, float population)
        {
            var predictionFunction = mlContext.Model.CreatePredictionEngine<EmissionModel, EmissionPrediction>(model);
            var test = new EmissionModel() { Year = year, Population = population };
            var prediction = predictionFunction.Predict(test);
            return new YearWithValue(year, new Wert(prediction.Co2, true));
        }
        //für alle möglichen gase
        public async Task<List<YearWithValue>> TrainLinearMoreInputsMLNETAsync(List<YearWithValue> ListWithCO, List<YearWithValue> Population, int FutureYear)
        {
            MLContext mlContext = new MLContext(seed: 0);
            ListWithCO = ListWithCO.Distinct().ToList();
            int coaid = await dB.GetCountryByNameAsync(ListWithCO.First(x => x.Name != null).Name);      //Inshallah ist in dieser liste nie kein name irgendwo
            int catid = ListWithCO.First(x => x.cat_id != 0).cat_id;
            List<EmissionModel> inputs = new List<EmissionModel>();
            if (!(Population.Count > 0)) //ohje
            {
                Console.WriteLine("Zu diesem Punkt im Programm sollte es eigentlich nie kommen. Ich hab aber keine Zeit, das ordentlich zu fixen. Darum hier diese Pfusch-Lösung mit dieser Ausgabe als Erinnerung, dass ich das gscheid behebe, wenn noch Zeit überbleibt");
                Population = await dB.GetPopulationByCoaIdAsync(coaid);
            }
            // ListWithCO = GetDistinctValues(ListWithCO);
            foreach (var JahrMitCO in ListWithCO)
            {
                float tempyear = JahrMitCO.Year;
                foreach (var JahrMitPopulation in Population)
                {
                    if (JahrMitPopulation.Year == tempyear)
                    {
                        inputs.Add(new EmissionModel() { Year = tempyear, Population = JahrMitPopulation.Value.value, Co2 = JahrMitCO.Value.value });
                    }
                }

            }
            Model modelContainer = Train(mlContext, inputs);
          //  dB.SaveModelAsParameter(modelContainer, coaid, catid, GetErrorMLNet(modelContainer));
            var model = modelContainer.trainedModel;
            double j = inputs.Max(x => x.Year); 
            if (j < FutureYear)
            {
                CNTK cNTK = new CNTK(dB);
                j++;
                ListWithCO = await PredictCo2OverYearsAsync(modelContainer, FutureYear, coaid, ListWithCO, cNTK);
            }
            dB.SaveModel(modelContainer, coaid, catid);
            return ListWithCO;
        }
        private static List<Input> NormalizeEmissionModel(List<EmissionModel> inputs, int futureYear)
        {
            List<YearWithValue> population = new List<YearWithValue>();
            List<YearWithValue> co2 = new List<YearWithValue>();
            foreach (var item in inputs)
            {
                if (!(co2.Any(y => y.Value.value == item.Co2)))
                {
                    co2.Add(new YearWithValue(item.Year, new Wert(item.Co2), "emission"));
                    if (!(population.Any(x => x.Value.value == item.Population)))
                    {
                        population.Add(new YearWithValue(item.Year, new Wert(item.Population)));
                    }
                }
            }

            List<Input> list = new List<Input>() { Standarization(population, futureYear), Standarization(co2, futureYear) };
            list[0].name = "population";
            list[1].name = "emission";
            return list;

        }
    }
}
