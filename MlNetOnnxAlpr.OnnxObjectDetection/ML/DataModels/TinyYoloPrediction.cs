using Microsoft.ML.Data;

namespace MLNetOnnxAlpr.OnnxObjectDetection.ML.DataModels
{
    public class TinyYoloPrediction : IOnnxObjectPrediction
    {
        [ColumnName("grid")] public float[] PredictedLabels { get; set; }
    }
}