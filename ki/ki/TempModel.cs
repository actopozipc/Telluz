using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    class TempModel
    {
        [LoadColumn(0)]
        public float longitude;
        [LoadColumn(1)]
        public float latitude;
        [LoadColumn(2)]
        public float lastYearValue;
        [LoadColumn(3)]
        public float year;
        [LoadColumn(4)]
        public float temp;

    }
    public class TempPrediction
    {
        [ColumnName("Score")]
        public float temp;
    }
}
