using Microsoft.ML.Data;

namespace ElectrostoreIA.Dto;

public class ImagePrediction
{
    [ColumnName("PredictedLabel")]
    public string? PredictedLabel { get; set; }
    public float[]? Score { get; set; }
}