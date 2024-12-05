using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
/* using Microsoft.ML;
using Microsoft.ML.Vision;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using Microsoft.ML.OnnxRuntime; */


namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<string, TrainingStatus> TrainingStatuses = new ConcurrentDictionary<string, TrainingStatus>();
    private static bool IsTrainingInProgress = false;

    public IAService(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<ReadIADto>> GetIA(int limit = 100, int offset = 0)
    {
        return await _context.IA
            .Skip(offset)
            .Take(limit)
            .Select(ia => new ReadIADto
            {
                id_ia = ia.id_ia,
                nom_ia = ia.nom_ia,
                description_ia = ia.description_ia,
                date_ia = ia.date_ia,
                trained_ia = ia.trained_ia
            }).ToListAsync();
    }

    public async Task<int> GetIACount()
    {
        return await _context.IA.CountAsync();
    }

    public async Task<ReadIADto> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        return new ReadIADto
        {
            id_ia = ia.id_ia,
            nom_ia = ia.nom_ia,
            description_ia = ia.description_ia,
            date_ia = ia.date_ia,
            trained_ia = ia.trained_ia
        };
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var newIA = new IA
        {
            nom_ia = iaDto.nom_ia,
            description_ia = iaDto.description_ia,
            date_ia = DateTime.Now
        };
        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();
        return new ReadIADto
        {
            id_ia = newIA.id_ia,
            nom_ia = newIA.nom_ia,
            description_ia = newIA.description_ia,
            date_ia = newIA.date_ia,
            trained_ia = newIA.trained_ia
        };
    }

    public async Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto)
    {
        var iaToUpdate = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        if (iaDto.nom_ia != null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia != null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        // if model exists set trained_ia to true
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && !iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = true;
            iaToUpdate.date_ia = DateTime.Now;
        }
        else if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = false;
        }
        await _context.SaveChangesAsync();
        return new ReadIADto
        {
            id_ia = iaToUpdate.id_ia,
            nom_ia = iaToUpdate.nom_ia,
            description_ia = iaToUpdate.description_ia,
            date_ia = iaToUpdate.date_ia,
            trained_ia = iaToUpdate.trained_ia
        };
    }

    public async Task DeleteIA(int id)
    {
        var iaToDelete = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        // remove model if exists
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras"));
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","ItemList" + id.ToString() + ".txt"));
        }

        _context.IA.Remove(iaToDelete);
        await _context.SaveChangesAsync();
    }

    /* public async Task<TrainingStatus> GetTrainingStatus(string id)
    {
        TrainingStatuses.TryGetValue(id, out var status);
        return status;
    } */

    /* public async Task<GetTrainStart> TrainIA(int id)
    {
        if (IsTrainingInProgress)
        {
            return new GetTrainStart { TrainStarted = false, msg = "Une autre IA est déjà en cours d'entraînement." };
        }

        var ia = await _context.IA.FindAsync(id);
        if (ia == null)
        {
            return new GetTrainStart { TrainStarted = false, msg = "IA non trouvée." };
        }
        ia.trained_ia = false; // Réinitialiser le statut de l'entraînement
        
        // remove model if exists
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".zip")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".zip"));
        }

        IsTrainingInProgress = true;
        TrainingStatuses[id.ToString()] = new TrainingStatus { Progress = 0, IsCompleted = false, IsRunning = true, Message = "Entraînement en cours" };

        _ = Task.Run(async () =>
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await TrainingAsync(id.ToString(), dbContext);
                }
            }
            finally
            {
                IsTrainingInProgress = false;
            }
        });
        await _context.SaveChangesAsync();
        return new GetTrainStart { TrainStarted = true, msg = "Entraînement démarré." };
    } */

    /* public async Task<ReadItemDto> DetectItem(int id, IFormFile imgToScan)
    {
        if (imgToScan == null || imgToScan.Length == 0)
        {
            
        }
        var ia = await _context.IA.FindAsync(id);
        if (ia == null || !ia.trained_ia)
        {
            
        }

        try
        {
            string tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await imgToScan.CopyToAsync(stream);
            }
            var imageData = new PredictionInput { url_img = tempFilePath };

            // load model
            var mlContext = new MLContext();
            ITransformer trainedModel = mlContext.Model.Load(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".zip"), out var modelSchema);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<PredictionInput, PredictionOutput>(trainedModel);
            var prediction = predictionEngine.Predict(imageData);

            Console.WriteLine($"id item find : {prediction.PredictedLabel}");
            for (int i = 0; i < prediction.Score.Length; i++)
            {
                Console.WriteLine($"Score for item {i} : {prediction.Score[i]}");
            }

            var item = await _context.Items.FindAsync(prediction.PredictedLabel);
            if (item == null)
            {
                
            }
            return new ReadItemDto
            {
                id_item = item.id_item,
                nom_item = item.nom_item,
                seuil_min_item = item.seuil_min_item,
                description_item = item.description_item,
                id_img = item.id_img
            };
        }
        catch (Exception ex)
        {
            // Log the exception details
        Console.WriteLine($"Error during detection: {ex.Message}, StackTrace: {ex.StackTrace}");
            
        }
    } */

    /* private async Task TrainingAsync(string id, ApplicationDbContext _contextBackend)
    {
        Console.WriteLine("Task Training started");
        // Charger les images depuis la table IAImgs et faire un inner join avec la table imgs
        var listImgs = await _contextBackend.IAImgs
            .Where(iaimg => iaimg.id_ia == int.Parse(id))
            .Join(_contextBackend.Imgs, iaimg => iaimg.id_img, img => img.id_img, (iaimg, img) => new TrainImageData 
            { 
                id_item = img.id_item, 
                url_img = img.url_img, 
                id_img = img.id_img 
            })
            .ToListAsync();

        try
        {
            // Préparer les données d'entraînement
            var mlContext = new MLContext();
            mlContext.Log += (sender, e) =>
            {
                Console.WriteLine($"[{e.Source}] {e.Message}");
            };
            var imageDataView = mlContext.Data.LoadFromEnumerable(listImgs);
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(TrainImageData.id_item))
                .Append(mlContext.Transforms.LoadImages(outputColumnName: "Image", inputColumnName: nameof(TrainImageData.url_img), imageFolder: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images")))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "Image", imageWidth: 224, imageHeight: 224, inputColumnName: "Image"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "Features", inputColumnName: "Image"))
                .Append(mlContext.MulticlassClassification.Trainers.SdcaNonCalibrated(labelColumnName: "Label", featureColumnName: "Features", maximumNumberOfIterations: 100))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            foreach (var image in listImgs)
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", image.url_img);
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine($"Image not found: {imagePath}");
                    throw new FileNotFoundException($"Image not found: {imagePath}");
                }
            }
            var model = pipeline.Fit(imageDataView);
            mlContext.Model.Save(model, imageDataView.Schema, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id + ".zip"));

            // Mettre à jour le statut de l'IA
            var ia = await _contextBackend.IA.FindAsync(int.Parse(id));
            if (ia == null)
            {
                Console.WriteLine("IA not found during training");
                throw new Exception("IA not found during training");
            }
            ia.trained_ia = true;
            await _contextBackend.SaveChangesAsync();
            
            // Mettre à jour le statut de l'entraînement
            TrainingStatuses[id].IsCompleted = true;
            TrainingStatuses[id].Progress = 100;
            TrainingStatuses[id].IsRunning = false;
            TrainingStatuses[id].Message = "Entraînement terminé";
        }
        catch (Exception ex)
        {
            // Log the exception details
            Console.WriteLine($"Error during training: {ex.Message}");
            // Mettre à jour le statut de l'entraînement
            TrainingStatuses[id].IsCompleted = false;
            TrainingStatuses[id].Progress = 0;
            TrainingStatuses[id].IsRunning = false;
            TrainingStatuses[id].Message = "Erreur lors de l'entraînement " + ex.Message;
            throw;
        }
    } */
}