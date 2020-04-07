
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
                            switch (request.type)
                            {
                                case type.Raw:
                                    liste = await TryConn(request.coa_id, request.cat_id, request.from, request.to);
                                    response = ConvertDataToResponse(liste);
                                    bf.Serialize(ns, response);
                                    PrintList(liste);
                                    break;
                                case type.Image:
                                    List<int> coaids = new List<int>();
                                    List<Response> responsesLocal = new List<Response>();
                                    if (Int32.TryParse(request.key, out int result)) //Falls Land über ID angefragt wird
                                    {
                                        coaids[0] = Convert.ToInt32(request.key);
                                    }
                                    else //falls Land über ISO code angefragt wird
                                    {
                                        //finde coa id zu ISO code 
                                        DB dB = new DB();
                                        switch (request.key)
                                        {
                                            case "Asia":

                                                break;
                                            case "Africa":
                                                coaids = new List<int>() { 3,15,17,18,22,33,40,41,42,43,45,47,55,59,66,68,71,79,82,84,85,86,87,120 };
                                                break;
                                            case "NorthAmerica":
                                                break;

                                            case "SouthAmerica":
                                                break;

                                            case "Antarctica":

                                                break;

                                            case "Europe":

                                                break;

                                            case "Australia":

                                                break;
                                            default:
                                                coaids[0] = await dB.GetCountryByKeyAsync(request.key);
                                                break;
                                        }
                                       
                                    }
                                    if (AllIdsGrZero(coaids)) //falls gültiger Iso Code
                                    {
                                       
                                        foreach (var item in coaids)
                                        {
                                            //TryConn bis zum gefragten jahr aufrufen
                                            liste = await TryConn(item, request.cat_id, request.from, request.to);
                                            //Wert(e) normieren
                                            //normiertes request.to returnen

                                            response = ConvertDataToResponse(liste);
                                            if (liste.Any(x => x.doesContainAnyValues()))
                                            {
                                                try
                                                {

                                              
                                                response.colorVal = GetColorValueFromOriginalValue(liste, request.to);
                                                }
                                                catch (AggregateException)
                                                {

                                                    response.errorMessage = "Country has no value in the specific year";
                                                }
                                            }
                                            else
                                            {
                                                response.errorMessage = "No values in category";
                                            }
                                            responsesLocal.Add(response);
                                        }


                                    }
                                    else //falls ungültiger ISO code
                                    {
                                        response.errorMessage = "Invalid ISO";
                                    }
                                    bf.Serialize(ns, responsesLocal);
                                    PrintList(liste);
                                    break;
                                case type.Everything:
                                    List<Response> responses = new List<Response>();
                                    List<Countrystats> locallist = new List<Countrystats>(); ;
                                    for (int i = 0; i < 264; i++)
                                    {
                                       
                                            locallist = await TryConn(i,4, request.from, request.to);
                                            responses.Add(ConvertDataToResponse(locallist));
                                           
                                    }
                                    bf.Serialize(ns, responses);
                                    PrintList(locallist);
                                    break;
                                default:
                                    break;
                            }
                          
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
            float x = 2 * 255 / max;
            return Convert.ToInt32(allValues.Find(a => a == a).ListWithCategoriesWithYearsAndValues.Find(b => b == b).YearsWithValues.First(c => c.Year == year).Value.value) * x;
        }
        private static async Task<List<Countrystats>> TryConn(int coa_id, int cat_id, int fromYear, int toYear)
        {
            try
            {
                CalculateData calc = new CalculateData();
                List<Countrystats> list;
                List<Countrystats> list2;
                if (cat_id > 38 && cat_id < 46)
                {
                    //pfusch but it works
                    list = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { 4 }, fromYear, toYear);
                    list2 = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { cat_id }, fromYear, toYear);

                    list[0].ListWithCategoriesWithYearsAndValues.Add(list2[0].ListWithCategoriesWithYearsAndValues[0]);
                }
                else
                {
                    list = await calc.GenerateForEachCountryAsync(new List<int>() { coa_id }, new List<int>() { cat_id }, fromYear, toYear);
                }
                return list;
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
        //Checks list of ids for invalid values
      public static bool AllIdsGrZero(List<int> ids)
        {
            foreach (var item in ids)
            {
                if (item<=0)
                {
                    return false;
                }
            }
            return true;
        }
        static void PrintList(List<Countrystats> liste)
        {
            foreach (var LandMitKategorieUndAllenDatenJahren in liste)
            {
                Printf("<======" + LandMitKategorieUndAllenDatenJahren.Country.name + "======>");

                foreach (var KategorieMitJahrenUndWerten in LandMitKategorieUndAllenDatenJahren.ListWithCategoriesWithYearsAndValues)
                {

                    if (KategorieMitJahrenUndWerten.category.name != null)
                    {
                        Printf(KategorieMitJahrenUndWerten.category.name);
                    }  

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
