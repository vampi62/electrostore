using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Microsoft.ML;
using Microsoft.ML.Vision;
using Microsoft.ML.Data;


namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly ApplicationDbContext _context;
    private static readonly ConcurrentDictionary<string, TrainingStatus> TrainingStatuses = new ConcurrentDictionary<string, TrainingStatus>();
    private static bool IsTrainingInProgress = false;

    public IAService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<ActionResult<ReadIADto>> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id);
        if (ia == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

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

    public async Task<ActionResult<ReadIADto>> UpdateIA(int id, UpdateIADto iaDto)
    {
        var iaToUpdata = await _context.IA.FindAsync(id);
        if (iaToUpdata == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

        if (iaDto.nom_ia != null)
        {
            iaToUpdata.nom_ia = iaDto.nom_ia;
        }

        if (iaDto.description_ia != null)
        {
            iaToUpdata.description_ia = iaDto.description_ia;
        }

        await _context.SaveChangesAsync();

        return new ReadIADto
        {
            id_ia = iaToUpdata.id_ia,
            nom_ia = iaToUpdata.nom_ia,
            description_ia = iaToUpdata.description_ia,
            date_ia = iaToUpdata.date_ia,
            trained_ia = iaToUpdata.trained_ia
        };
    }

    public async Task<IActionResult> DeleteIA(int id)
    {
        var iaToDelete = await _context.IA.FindAsync(id);
        if (iaToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

        _context.IA.Remove(iaToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<TrainingStatus> GetTrainingStatus(string id)
    {
        TrainingStatuses.TryGetValue(id, out var status);
        return status;
    }

    public async Task<GetTrainStart> TrainIA(int id)
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
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/models", id.ToString() + ".h5")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/models", id.ToString() + ".h5"));
        }

        IsTrainingInProgress = true;
        TrainingStatuses[id.ToString()] = new TrainingStatus { Progress = 0, IsCompleted = false };

        _ = Task.Run(async () =>
        {
            try
            {
                await TrainingAsync(id.ToString());
            }
            finally
            {
                IsTrainingInProgress = false;
            }
        });
        await _context.SaveChangesAsync();
        return new GetTrainStart { TrainStarted = true, msg = "Entraînement démarré." };
    }

    public async Task<ActionResult<ReadItemDto>> DetectItem(int id, IFormFile imgToScan)
    {
        if (imgToScan == null || imgToScan.Length == 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { imgToScan = new string[] { "Image not found" } }});
        }
        var ia = await _context.IA.FindAsync(id);
        if (ia == null || !ia.trained_ia)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found or not trained" } }});
        }

        try
        {
            // load model
            var mlContext = new MLContext();
            var model = mlContext.Model.Load("model" + id.ToString() + ".zip", out var schema);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);

            // predict
            var imageData = new byte[imgToScan.Length];
            await imgToScan.OpenReadStream().ReadAsync(imageData);
            var data = new ImageData { Image = imageData };
            var prediction = predictionEngine.Predict(data);
            var item = await _context.Items.FindAsync(prediction.id_item);
            if (item == null)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { imgToScan = new string[] { "Item not found" } }});
            }
            return new ReadItemDto
            {
                id_item = item.id_item,
                nom_item = item.nom_item,
                seuil_min_item = item.seuil_min_item,
                datasheet_item = item.datasheet_item,
                description_item = item.description_item,
                id_img = item.id_img
            };
        }
        catch (Exception ex)
        {
            // Log the exception details
            Console.WriteLine($"Error during detection: {ex.Message}");
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { imgToScan = new string[] { "Error during detection" } }});
        }
    }

    private async Task TrainingAsync(string id)
    {
        // Charger les images depuis la table IAImgs et faire un inner join avec la table imgs
        var listImgs = await _context.IAImgs
            .Where(iaImg => iaImg.id_ia == int.Parse(id))
            .Join(_context.Imgs, iaImg => iaImg.id_img, img => img.id_img, (iaImg, img) => new { iaImg, img })
            .Select(x => new { x.img.id_img, x.img.url_img, x.img.id_item })
            .ToListAsync();

        try
        {
            // Préparer les données d'entraînement
            var mlContext = new MLContext();
            var imageDataView = mlContext.Data.LoadFromEnumerable(listImgs);
            /* var pipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: nameof(ImageData.id_item))
                .Append(mlContext.Transforms.LoadRawImageBytes(outputColumnName: "Image", imageFolder: "wwwroot" + Path.DirectorySeparatorChar + "images", inputColumnName: nameof(ImageData.url_img)))
                .Append(mlContext.MulticlassClassification.Trainers.ImageClassification("LabelKey"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));
             */
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: nameof(ImageData.id_item))
                .Append(mlContext.Transforms.LoadImages(outputColumnName: "Image", imageFolder: "wwwroot" + Path.DirectorySeparatorChar + "images", inputColumnName: nameof(ImageData.url_img)))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "Image", imageWidth: 224, imageHeight: 224, inputColumnName: "Image"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "Image"))
                .Append(mlContext.MulticlassClassification.Trainers.ImageClassification("LabelKey"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));

            var model = pipeline.Fit(imageDataView);
            mlContext.Model.Save(model, imageDataView.Schema, "Model" + id + ".zip");

            // Mettre à jour le statut de l'IA
            var ia = await _context.IA.FindAsync(int.Parse(id));
            if (ia == null)
            {
                Console.WriteLine("IA not found during training");
                throw new Exception("IA not found during training");
            }
            ia.trained_ia = true;
            await _context.SaveChangesAsync();
            
            // Mettre à jour le statut de l'entraînement
            TrainingStatuses[id].IsCompleted = true;
            TrainingStatuses[id].Progress = 100;
        }
        catch (Exception ex)
        {
            // Log the exception details
            Console.WriteLine($"Error during training: {ex.Message}");
            throw;
        }

        // Mettre à jour le statut de l'entraînement
        TrainingStatuses[id].IsCompleted = true;
        TrainingStatuses[id].Progress = 100;

    }

}