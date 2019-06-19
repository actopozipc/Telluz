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
            Console.Title = "GreenVision";
            DatenBerechnung DB = new DatenBerechnung();
            foreach (var item in DB.GeneriereVorhersage())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(item.description);
                Console.ForegroundColor = ConsoleColor.White;
                foreach (var it in item.JahreMitWerten)
                {
                    Console.WriteLine($"{it.year} : {it.value}");
                }

            }
            Console.ReadKey();
        }
       
    }
}
