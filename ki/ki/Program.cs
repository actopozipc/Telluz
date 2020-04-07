
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using telluz;

namespace ki
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.Title = "Telluz";
            Console.WriteLine("Gestartet um " + DateTime.Now);
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

                using (StreamWriter swt = new StreamWriter($"{DateTime.Now.ToString().Replace('.', '-').Replace(':', '-')}.txt"))
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
                                    DB dB = new DB();
                                    List<int> coaids = new List<int>();
                                    List<Response> responsesLocal = new List<Response>();
                                    if (Int32.TryParse(request.key, out int result)) //Falls Land über ID angefragt wird
                                    {
                                        coaids[0] = Convert.ToInt32(request.key);
                                    }
                                    else //falls Land über ISO code angefragt wird
                                    {
                                        //finde coa id zu ISO code 

                                        switch (request.key)
                                        {
                                            case "Asia":

                                                break;
                                            case "Africa":
                                                coaids = new List<int>() { 3, 15, 17, 18, 22, 33, 40, 41, 42, 43, 45, 47, 55, 59, 66, 68, 71, 79, 82, 84, 85, 86, 87, 120 };
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
                                                var coaid = await dB.GetCountryByKeyAsync(request.key);
                                                coaids.Add(coaid);
                                                Console.WriteLine(coaids[0]);
                                                break;
                                        }

                                    }
                                    if (AllIdsGrZero(coaids)) //falls gültiger Iso Code
                                    {
                                        if (coaids.Count > 1) //Falls es für mehrere Kategorien ist
                                        {
                                            List<List<Countrystats>> countrystats = new List<List<Countrystats>>();
                                            foreach (var item in coaids)
                                            {
                                                liste = await TryConn(item, request.cat_id, request.from, request.to);
                                                countrystats.Add(liste);
                                                response = ConvertDataToResponse(liste);
                                                response.coa = await dB.GetCountryNameByIDAsync(item);
                                                responsesLocal.Add(response);
                                            }

                                            GetColorValueFromAllOriginalValues(countrystats, request.to, responsesLocal);

                                        }
                                        else //falls colorvalue nur für eine kategorie ausgerechnet wird
                                        {
                                            //TryConn bis zum gefragten jahr aufrufen
                                            liste = await TryConn(coaids[0], request.cat_id, request.from, request.to);
                                            //Wert(e) normieren
                                            //normiertes request.to returnen
                                            response = ConvertDataToResponse(liste);
                                            if (liste.Any(x => x.doesContainAnyValues()))
                                            {
                                                try
                                                {
                                                    response.colorVal = GetColorValueFromOriginalValue(liste, request.to);
                                                }
                                                catch (InvalidOperationException)
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

                                        locallist = await TryConn(i, 4, request.from, request.to);
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
        private static void GetColorValueFromAllOriginalValues(List<List<Countrystats>> allAllValues, int year, List<Response> responses)
        {
            //Find all values for year
            List<YearWithValue> valuesForYear = new List<YearWithValue>();
            foreach (var valuesForOnecountry in allAllValues)
            {
                if (valuesForOnecountry.Any(x => x.ListWithCategoriesWithYearsAndValues.Count > 1))
                {
                    Console.WriteLine("sucks");
                }
                if (valuesForOnecountry.First().doesContainAnyValues())
                {

                    Countrystats country = valuesForOnecountry.First();
                    CategoriesWithYearsAndValues categoriesWithYearsAndValues = country.ListWithCategoriesWithYearsAndValues.Find(z => z.YearsWithValues.Any(b => b.Year == year));
                    if (categoriesWithYearsAndValues != null)
                    {


                        var yearWithValues = categoriesWithYearsAndValues.YearsWithValues;
                        var valueForYear = yearWithValues.Find(x => x.Year == year);
                        //  var valueForYear = valuesForOnecountry.First(y => y == y).ListWithCategoriesWithYearsAndValues.Find(z => z.YearsWithValues.Any(b => b.Year == year)).YearsWithValues.First(a => a.Year == year);
                        valuesForYear.Add(valueForYear);
                        //Find max of all values in year
                        float maxValue = valuesForYear.Where(x => x == x).Max(y => y.Value.value);
                        //Berechne color-Value für alles und weise es den korrekten responses zu
                        //calc color-value
                        float m = 2 * 255 / maxValue;
                        foreach (var item in valuesForYear)
                        {
                            try
                            {

                                int index = responses.FindIndex(x => x.valuePairList.Any(y => y.value == item.Value.value)); //Get first value-year list where any value is the same as one of the values in the list with values for year
                                var temp = responses[index];                                  
                                 temp.colorVal = m * temp.valuePairList.First(x => x.year == year).value;
                                if (temp.colorVal<0)
                                {
                                    temp.errorMessage = "colorValue below 0, ich fix es später";
                                    temp.colorVal = 1;
                                }
                                responses[index] = temp;
                            }
                            catch (InvalidOperationException)
                            {
                                Console.WriteLine("Meier, kann nicht besser nach Kategorien filtern wo keine Werte drin sind");

                            }

                        }

                    }
                }

            }

            var countriesWithNoValuesInYear = responses.Where(x => x.colorVal == 0).ToArray();
            for (int i = 0; i < countriesWithNoValuesInYear.Count(); i++)
            {
                countriesWithNoValuesInYear[i].errorMessage = "Country has no value in the specific year";
            }
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
            if (list.Any(x => x.ListWithCategoriesWithYearsAndValues.Any(y => y.YearsWithValues.Count > 0)))
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
            response.valuePairList = valuePairs;
            return response;
        }
        //Checks list of ids for invalid values
        public static bool AllIdsGrZero(List<int> ids)
        {
            foreach (var item in ids)
            {
                if (item <= 0)
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
