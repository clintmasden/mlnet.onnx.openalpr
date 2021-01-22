using Microsoft.ML.Data;

namespace MLNetOnnxAlpr.OnnxObjectDetection.ML.DataModels
{
    public class CustomVisionPrediction : IOnnxObjectPrediction
    {
        [ColumnName("model_outputs0")] public float[] PredictedLabels { get; set; }
    }
}