using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class KategorieMitJahrenUndWerten
    {
        public string description { get; set; }
        public List<JahrMitWert> JahreMitWerten { get; set; }
        public KategorieMitJahrenUndWerten(string d, List<JahrMitWert> J)
        {
            description = d;
            JahreMitWerten = J;
        }
        public KategorieMitJahrenUndWerten(string d, Task<List<JahrMitWert>> J)
        {
            description = d;
            JahreMitWerten = J.Result;
        }
        //Konstruktor nicht löschen
        public KategorieMitJahrenUndWerten()
        {

        }
    }
}
