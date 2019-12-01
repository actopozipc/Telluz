using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class Country
    {
      public  string name;
        public float longitude { get; private set; }
        public float latitude { get; private set; }

        public Country(string name)
        {
            this.name = name;
        }
        public Country(string name, float longitude, float latitude)
        {
            this.name = name;
            this.longitude = longitude;
            this.latitude = latitude;
        }

    }
}
