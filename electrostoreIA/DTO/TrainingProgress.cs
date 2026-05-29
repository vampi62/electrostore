using ElectrostoreIA.Enums;

namespace ElectrostoreIA.Dto;

public class TrainingProgress
{
    public TrainingStatus Status { get; set; } = TrainingStatus.NotPlanned;
    public string Message { get; set; } = "Training not planned";
    public int Epoch { get; set; } = 0;
    public float Accuracy { get; set; } = 0;
    public float ValAccuracy { get; set; } = 0;
    public float Loss { get; set; } = 0;
    public float ValLoss { get; set; } = 0;
    public float LearningRate { get; set; } = 0;
    public int BatchProcessedCount { get; set; } = 0;
    public int RequestedBy { get; set; } = 0;
}