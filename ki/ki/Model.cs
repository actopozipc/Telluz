using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ki
{
    /// <summary>
    /// Zum Speichern von ML.NET Parametern
    /// </summary>
    class Model
    {
       public ITransformer trainedModel { get; set; }
        IEstimator<ITransformer> pipeline { get; set; }
        public IDataView data { get; set; }
      public  MLContext mLContext { get; set; }

        public Model(ITransformer transformer, IEstimator<ITransformer> pipeline, MLContext mLContext, IDataView data)
        {
            this.trainedModel = transformer;
            this.pipeline = pipeline;
            this.mLContext = mLContext;
            this.data = data;
        }
    }
}
