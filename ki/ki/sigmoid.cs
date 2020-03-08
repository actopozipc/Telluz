using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class sigmoid
    {
        const int x = 210;
        /// <summary>
        /// Sieht anhand einer Liste mit Jahren und Zahlen den Wert für Jahr Ziel voraus
        /// </summary>
        /// <param name="KnownValues">Liste bekannter Werte anhand deren die KI lernt</param>
        /// <param name="FutureYear"> Bis zu welchem Jahr die KI werte vorhersagen soll</param>
        /// <param name="multi">Wie viel man normierte Werte mulitplizieren muss</param>
        /// <returns>Liste mit allen bereits bekannten Werten + Vorhersagen für zukünftige Werte</returns>
        public static List<YearWithValue> TrainSigmoid(List<YearWithValue> KnownValues, int FutureYear, int multi)
        {

            List<double> inputs = new List<double>(); //Jahre
            List<double> outputs = new List<double>(); //Werte 
                                                       //Für jedes Jahr mit Wert in der Liste mit Jahren und Werten d
            foreach (var YearWithValue in KnownValues)
            {
                inputs.Add(Convert.ToDouble(YearWithValue.Year));
                outputs.Add(Convert.ToDouble(YearWithValue.Value.value) / (Math.Pow(10, multi)));
            }
            Input input = AI.StandardizationYears(inputs, FutureYear);
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
                    hiddenNeuron1.inputs = input.GetNormYear(inputs[i]);
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
                hiddenNeuron1.inputs = input.GetNormYear(j) + input.step;
                outputNeuron.inputs = hiddenNeuron1.output;
                KnownValues.Add(new YearWithValue((Math.Round((inputs[inputs.Count - 1] + 1))), new Wert((float)(outputNeuron.output * Convert.ToDouble(Math.Pow(10, multi))), true)));
                return TrainSigmoid(KnownValues, FutureYear, multi);
            }
            //wenn alle Jahre bekannt sind, returne die Liste
            else
            {
                return KnownValues;
            }

        
        /// <summary>
        /// checks, if the biggest year in a list of YearWithValue is bigger than the given value FutureYear
        /// not much code, but it appears often enough to outsource it
        /// </summary>
        /// <returns></returns>

    }
    //https://de.wikipedia.org/wiki/Sigmoidfunktion
    public static double output(double x)
        {
            double temp = 1 / (1.0 + Math.Exp(-x));

            return temp;

        }

        public static double derived(double x)
        {
            return output(x) * (1 - output(x));
        }
    }
}
