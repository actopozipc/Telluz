
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using telluz;

namespace ki
{
    class Program
    {
     
        static void Main(string[] args)
        {
            Console.Title = "Telluz"; 
            Console.WriteLine("Gestartet um " +DateTime.Now);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {

            
            Task.Run(async () =>
            {
             await Listen();
            
            }).GetAwaiter().GetResult();
               
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            Console.ReadKey();
        
            }
            catch (AggregateException ex)
            {

                Console.WriteLine(ex.Flatten());
            }

            Console.ReadKey();
        }
        public static async Task Listen()
        {
            TcpListener server = new TcpListener(IPAddress.Loopback, 210);

            server.Start();
            Console.WriteLine("Server gestartet");
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            while (true)
            {
                Console.WriteLine("Warte auf Request");
                using (TcpClient client = await server.AcceptTcpClientAsync())
                {
                    Console.WriteLine("Request empfangen");
                    using (NetworkStream ns = client.GetStream())
                    {
                        Request request = (Request)binaryFormatter.Deserialize(ns);
                        List<Countrystats> liste;
                     
                        switch (request.typ)
                        {
                            case type.Daten:
                                liste = await TryConn(request.coa_id, request.cat_id, request.to);
                                BinaryFormatter bf = new BinaryFormatter();
                                List<Respond> responds = ConvertDataToRespond(liste);
                                bf.Serialize(ns, responds);
                                break;
                            case type.Bild: //Hier wird später noch nur ein jahr ein wert zurück gegeben
                                //finde coa id zu ISO code 
                                //TryConn bis zum gefragten jahr aufrufen
                                //Wert(e) normieren
                                //normiertes request.to returnen
                                GetColorValueFromOriginalValue(liste, request.to);
                                 bf = new BinaryFormatter();
                                 responds = ConvertDataToRespond(liste);
                                bf.Serialize(ns, responds);
                                break;
                            default:
                                break;
                        }
                       
                        PrintList(liste);
                    }
                }
            }
        }
        //inshallah ist hier nie mehr als eine kategorie und ein land
        private static int GetColorValueFromOriginalValue(List<Countrystats> allValues, int year)
        {
            var max = allValues.Find(y => y == y).ListWithCategoriesWithYearsAndValues.Find(z => z == z).YearsWithValues.Max(a => a.Value).value;
            int x = 2 * 255 / Convert.ToInt32(max);
            return Convert.ToInt32(allValues.Find(a => a == a).ListWithCategoriesWithYearsAndValues.Find(b => b == b).YearsWithValues.First(c => c.Year == year).Value.value) * x;
        }
        private static async Task<List<Countrystats>> TryConn(int coa_id, int cat_id, int year)
        {
            try
            {
                CalculateData calc = new CalculateData();
             var   liste = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { cat_id }, year);
                return liste;
            }
            catch (Exception)
            {

           var     liste = new List<Countrystats>() { new Countrystats() { Country = new Country("austria"), ListWithCategoriesWithYearsAndValues = new List<CategoriesWithYearsAndValues>() { new CategoriesWithYearsAndValues() { YearsWithValues = new List<YearWithValue>() { new YearWithValue() { Year = 1960, Value = new Wert(1000f) } } } } } };
                return liste;
            }
        }
        private static List<Respond> ConvertDataToRespond(List<Countrystats> list)
        {
            List<Respond> responds = new List<Respond>();
            foreach (var kategorieMitJahrenWerten in list)
            {
                for (int i = 0; i < kategorieMitJahrenWerten.ListWithCategoriesWithYearsAndValues.Count; i++)
                {
                    foreach (var jahreMitWerten in kategorieMitJahrenWerten.ListWithCategoriesWithYearsAndValues[i].YearsWithValues)
                    {
                       
                        responds.Add(new Respond() { value = jahreMitWerten.Value.value, year = jahreMitWerten.Year, berechnet = jahreMitWerten.Value.berechnet});
                    }
                }
              
            }
            return responds;
        }
      
        static void PrintList(List<Countrystats> liste)
        {
            foreach (var LandMitKategorieUndAllenDatenJahren in liste)
            {
                Printf("<======" + LandMitKategorieUndAllenDatenJahren.Country + "======>");

                foreach (var KategorieMitJahrenUndWerten in LandMitKategorieUndAllenDatenJahren.ListWithCategoriesWithYearsAndValues)
                {

                    Printf(KategorieMitJahrenUndWerten.category);

                    foreach (var JahreMitWerten in KategorieMitJahrenUndWerten.YearsWithValues)
                    {
                        Console.WriteLine($"{JahreMitWerten.Year} : {JahreMitWerten.Value.value}");
                    }
                }
            }
        }
        static void Printf(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
       
    }
}
