
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
            Console.Title = "Gaiaz"; //wir brauchen einen besseren namen
            Console.WriteLine("Gestartet um " +DateTime.Now);
            Stopwatch sw = new Stopwatch();
            sw.Start();
           var liste =  Task.Run(async () =>
            {
               return await methodeAsync();
            }).GetAwaiter().GetResult();
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            Console.ReadKey();
            foreach (var LandMitKategorieUndAllenDatenJahren in liste)
            {
                foreach (var KategorieMitJahrenUndWerten in LandMitKategorieUndAllenDatenJahren.ListWithCategoriesWithYearsAndValues)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(KategorieMitJahrenUndWerten.category);
                    Console.ForegroundColor = ConsoleColor.White;
                    foreach (var JahreMitWerten in KategorieMitJahrenUndWerten.YearsWithValues)
                    {
                        Console.WriteLine($"{JahreMitWerten.year} : {JahreMitWerten.value}");
                    }
                }
            }
        }
        static async Task<List<Countrystats>> methodeAsync()
        {
          
            CalculateData calc = new CalculateData();
          
        var liste=    await calc.GenerateForEachCountryAsync();
            return liste;
           
            
            
        }
       
    }
}
