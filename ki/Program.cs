
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
            Console.Title = "GreenVision"; //wir brauchen einen besseren namen
            CalculateData calc = new CalculateData();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            calc.GenerateForEachCountry();
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            //foreach (var LandMitKategorieUndAllenDatenJahren in calc.GenerateForEachCountry())
            //{
            //    foreach (var KategorieMitJahrenUndWerten in LandMitKategorieUndAllenDatenJahren.ListWithCategoriesWithYearsAndValues)
            //    {
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        Console.WriteLine(KategorieMitJahrenUndWerten.category);
            //        Console.ForegroundColor = ConsoleColor.White;
            //        foreach (var JahreMitWerten in KategorieMitJahrenUndWerten.YearsWithValues)
            //        {
            //            Console.WriteLine($"{JahreMitWerten.year} : {JahreMitWerten.value}");
            //        }
            //    }
            //}
            Console.ReadKey();
        }
       
    }
}
