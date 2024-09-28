using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadIADto
{
    public int id_ia { get; init; }
    public string nom_ia { get; init; }
    public string description_ia { get; init; }
    public DateTime date_ia { get; init; }
    public bool trained_ia { get; init; }
}
public record CreateIADto
{
    [Required] public string nom_ia { get; init; }
    [Required] public string description_ia { get; init; }
}
public record UpdateIADto
{
    public string? nom_ia { get; init; }
    public string? description_ia { get; init; }
}

public class TrainingStatus
{
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public string? Message { get; set; }
    public bool IsRunning { get; set; }
}

public class GetTrainStart
{
    public bool TrainStarted { get; set; }
    public string msg { get; set; }
}

public record DetecDto
{
    [Required] public IFormFile img_file { get; init; }
}
/* 
public class TrainImageData
{
    public int id_img { get; set; }
    public string url_img { get; set; }
    public int id_item { get; set; }
}

public class PredictionInput
{
    public int id_img { get; set; }
    public string url_img { get; set; }
    public int id_item { get; set; }
}
public class PredictionOutput
{
    public int PredictedLabel { get; set; }
    public float[] Score { get; set; }
} */