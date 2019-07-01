using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    /// <summary>
    /// Backwardlearning ist eine überwachte Lernmethode 
    /// (=es gibt immer ein Ergebnis für die KI, wie es sein soll)
    /// Damit werden neurale Netzwerke gelehrt um neue Werte zu berechnen
    /// Zuerst werden zufällige Gewichte als startwert angenommen
    /// dann wird ein output gezeigt und die gewichte werden anhand des fehlers angepasst
    /// das ganze geht von vorne los
    /// </summary>
    class Neuron
    {
        public double inputs; //da kommt immer das jahr rein
        public double weights; //das ist wie wichtig das jahr für das ergebnis ist
        public double error; //fehler
        int multiplikator; //variable zum normieren von outputs
        private double biasWeight;

        private Random r = new Random();
        public Neuron()
        {

        }
        //konstruktor falls ich multiplikator direkt beim deklarieren angeben möchte, brauch ich glaub ich nimmer
        public Neuron(int m)
        {
            multiplikator = m;
        }
        public double output
        {
             //get { return sigmoid.output(weights[0] * inputs[0] + weights[1] * inputs[1] + biasWeight); }
           get { return sigmoid.output(weights * inputs + biasWeight); }
        }
        //wird beim erstellen der neuronen aufgerufen um mit einem wert zu starten
        public void randomizeWeights()
        {
            weights = r.NextDouble();
            biasWeight = r.NextDouble();
        }

        public void adjustWeights()
        {
            weights += error * inputs;
            biasWeight += error;
        }
    }
}
