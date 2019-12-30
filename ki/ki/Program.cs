
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

            
            var liste =  Task.Run(async () =>
            {
             await Listen();
               return await AsynchroneMainMethodenWerdenErstAbCsharp7ImplementiertAsync();
            }).GetAwaiter().GetResult();
               
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            Console.ReadKey();
            foreach (var LandMitKategorieUndAllenDatenJahren in liste)
            {
                Printf("<======"+LandMitKategorieUndAllenDatenJahren.Country+"======>");

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
                        Console.WriteLine();
                    }
                }
            }
        }
        static async Task<List<Countrystats>> AsynchroneMainMethodenWerdenErstAbCsharp7ImplementiertAsync()
        {
          

            CalculateData calc = new CalculateData();
            Random r = new Random();
           
             var liste = await calc.GenerateForEachCountryAsync(new List<int>() { r.Next(0,264) }, new List<int>() { 4,41 }, 2030);
            return liste;
            
        }
        static void Printf(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
       
    }
}
