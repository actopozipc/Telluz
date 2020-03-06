
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
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

                }).Wait();
               
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            Console.ReadKey();
        
            }
            catch (AggregateException ex)
            {
               
                using (StreamWriter swt = new StreamWriter($"{DateTime.Now.ToString().Replace('.','-').Replace(':','-')}.txt"))
                {
                    swt.Write(ex.Flatten());
                }
                Console.WriteLine(ex.Flatten());
            }

            Main(args);
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
                        Error err = request.checkForError();
                        if (err.hasErrors)
                        {
                            binaryFormatter.Serialize(ns, new List<Response>() { new Response(err) });
                            client.Client.Shutdown(SocketShutdown.Receive);
                            Console.WriteLine(err.errorMessage);
                        }
                        else
                        {
                            List<Countrystats> liste = new List<Countrystats>();
                            Response response = new Response();
                            BinaryFormatter bf = new BinaryFormatter();
                            switch (request.typ)
                            {
                                case type.Daten:
                                    liste = await TryConn(request.coa_id, request.cat_id, request.from, request.to);
                                    response = ConvertDataToResponse(liste);
                                    break;
                                case type.Bild:
                                    int coaid;
                                    if (Int32.TryParse(request.key, out int result)) //Falls Land über ID angefragt wird
                                    {
                                        coaid = Convert.ToInt32(request.key);
                                    }
                                    else //falls Land über ISO code angefragt wird
                                    {
                                        //finde coa id zu ISO code 
                                        DB dB = new DB();
                                        coaid = await dB.GetCountryByKeyAsync(request.key);
                                    }
                                    if (coaid > 0) //falls gültiger Iso Code
                                    {
                                        //TryConn bis zum gefragten jahr aufrufen
                                        liste = await TryConn(coaid, request.cat_id, request.from, request.to);
                                        //Wert(e) normieren
                                        //normiertes request.to returnen
                                    
                                        response = ConvertDataToResponse(liste);
                                        if (liste.Any(x=>x.doesContainAnyValues()))
                                        {
                                            response.colorVal = GetColorValueFromOriginalValue(liste, request.to);
                                        }
                                        else
                                        {
                                            response.errorMessage = "No values in category";
                                        }
                                       


                                    }
                                    else //falls ungültiger ISO code
                                    {
                                        response.errorMessage = "Invalid ISO";
                                    }
                                   
                                    break;
                                default:
                                    break;
                            }
                            bf.Serialize(ns, response);
                            PrintList(liste);
                        }
                        
                    }
                    client.Close();
                }
            }
        }
        //inshallah ist hier nie mehr als eine kategorie und ein land
        private static double GetColorValueFromOriginalValue(List<Countrystats> allValues, int year)
        {
            var max = allValues.Find(y => y == y).ListWithCategoriesWithYearsAndValues.Find(z => z == z).YearsWithValues.Max(a => a.Value.value);
            float x = 2 * 255 / Convert.ToInt32(max);
            return Convert.ToInt32(allValues.Find(a => a == a).ListWithCategoriesWithYearsAndValues.Find(b => b == b).YearsWithValues.First(c => c.Year == year).Value.value) * x;
        }
        private static async Task<List<Countrystats>> TryConn(int coa_id, int cat_id, int fromYear, int toYear)
        {
            try
            {
                CalculateData calc = new CalculateData();
                List<Countrystats> liste;
                if (cat_id > 38 && cat_id < 46)
                {
                    liste = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { 4, cat_id }, fromYear, toYear);
                }
                else
                {
                    liste = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { cat_id }, fromYear, toYear);
                }
                return liste;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                var liste = new List<Countrystats>() { new Countrystats() { Country = new Country("austria"), ListWithCategoriesWithYearsAndValues = new List<CategoriesWithYearsAndValues>() { new CategoriesWithYearsAndValues() { YearsWithValues = new List<YearWithValue>() { new YearWithValue() { Year = 1960, Value = new Wert(1000f) } } } } } };
                return liste;
            }
        }
        private static Response ConvertDataToResponse(List<Countrystats> list)
        {
            Response response = new Response();
            List<Response.ValuePair> valuePairs = new List<Response.ValuePair>();
            List<Response> listWithResponse = new List<Response>();
            if (list.Any(x=>x.ListWithCategoriesWithYearsAndValues.Any(y=>y.YearsWithValues.Count>0)))
            {
                foreach (var kategorieMitJahrenWerten in list)
                {
                    for (int i = 0; i < kategorieMitJahrenWerten.ListWithCategoriesWithYearsAndValues.Count; i++)
                    {
                        foreach (var jahreMitWerten in kategorieMitJahrenWerten.ListWithCategoriesWithYearsAndValues[i].YearsWithValues)
                        {
                            valuePairs.Add(new Response.ValuePair(jahreMitWerten.Value.berechnet, jahreMitWerten.Year, jahreMitWerten.Value.value));
                       
                        }
                    }

                }
            }
            else
            {
                listWithResponse.Add(new Response());
            }
            response.valuePair = valuePairs;
             return response;
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
