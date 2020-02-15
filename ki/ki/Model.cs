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
 
        public IDataView data { get; set; }
      public  MLContext mLContext { get; set; }

        public Model(ITransformer trainedModel, MLContext mLContext, IDataView data)
        {
            this.trainedModel = trainedModel;
     
            this.mLContext = mLContext;
            this.data = data;
        }
        public Model(ITransformer transformer, MLContext mLContext)
        {
            this.trainedModel = transformer;
            this.mLContext = mLContext;
        }
    }
}
