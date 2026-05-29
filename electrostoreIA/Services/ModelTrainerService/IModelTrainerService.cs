using ElectrostoreIA.Dto;


namespace ElectrostoreIA.Services.ModelTrainerService;

public interface IModelTrainerService
{
    public bool IsTrainingInProgress();
    public TrainingProgress? GetTrainingStatus(int idModel);
    public bool SetTrainingProgressMap(int idModel, TrainingProgress trainingProgress);
    public Task<bool> StartAndWaitAsync(int idModel, int requestedBy, CancellationToken ct = default);
    public string GetModelPath(int idModel);
    public string GetClassNamesPath(int idModel);
    public Task<string> EnsureModelLocalAsync(int idModel);
    public Task<string> EnsureClassNamesLocalAsync(int idModel);
    public Task DeleteModelFilesAsync(int idModel, CancellationToken ct = default);
}