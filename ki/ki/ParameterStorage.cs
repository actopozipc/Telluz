using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class ParameterStorage
    {
        public bool containsParameter;
      public  float W;
       public float b;
        public ParameterStorage(float W, float b)
        {
            this.W = W;
            this.b = b;
            containsParameter = true;
        }
        public ParameterStorage()
        {
            containsParameter = false;
        }
      public string GetParameterAsString()
        {
            return "W=" + W.ToString() + ";" + "b=" + b.ToString() + ";";
        }
    }
}
