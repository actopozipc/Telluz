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
        float W;
        float b;
        public ParameterStorage(float W, float b)
        {
            this.W = W;
            this.b = b;
            containsParameter = true;
        }
        public ParameterStorage()
        {

        }
       public static ParameterStorage WithoutParameter()
        {
            return new ParameterStorage() { containsParameter = false };
        }
    }
}
