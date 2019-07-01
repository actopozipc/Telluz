using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
            foreach (var item in calc.Generate())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(item.category);
                Console.ForegroundColor = ConsoleColor.White;
                foreach (var it in item.YearsWithValues)
                {
                    Console.WriteLine($"{it.year} : {it.value}");
                }

            }
            Console.ReadKey();
        }
       
    }
}
