
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var liste =  Task.Run(async () =>
            {
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
                        Console.WriteLine($"{JahreMitWerten.Year} : {JahreMitWerten.Value}");
                    }
                }
            }
        }
        static async Task<List<Countrystats>> AsynchroneMainMethodenWerdenErstAbCsharp7ImplementiertAsync()
        {
          
            CalculateData calc = new CalculateData();
          
             var liste = await calc.GenerateForEachCountryAsync(new List<int>(), new List<int>());
            return liste;
            
        }
        static void Printf(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
       
    }
}
