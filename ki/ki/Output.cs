using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ki
{
  public class Output
    {
        public List<float> outputs;
        [ColumnName("Score")]
        public float output;
    }
}
