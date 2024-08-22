using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Tensorflow;
using Keras.Models;
using Keras.Layers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

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
        var ia = await _context.IA.FindAsync(id);
        if (ia == null || !ia.trained_ia)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found or not trained" } }});
        }

        // load model
        

        // detect item



        return new ReadItemDto
        {
            id_item = 0,
            nom_item = "0",
            seuil_min_item = 0,
            datasheet_item = "0",
            description_item = "0",
            id_img = 0
        };
    }

    private async Task TrainingAsync(string id)
    {
        // Charger les images depuis la table IAImgs et faire un inner join avec la table imgs
        var listImgs = await _context.IAImgs
            .Where(iaImg => iaImg.id_ia == int.Parse(id))
            .Join(_context.Imgs, iaImg => iaImg.id_img, img => img.id_img, (iaImg, img) => new { iaImg, img })
            .Select(x => new { x.img.id_img, x.img.url_img, x.img.id_item })
            .ToListAsync();

        // Préparer les données d'entraînement
        var images = new List<float[]>();
        var labels = new List<int>();
        foreach (var img in listImgs)
        {
            images.Add(LoadImage(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", img.url_img)));
            labels.Add(img.id_item); // l'id_item représente l'objet à détecter
        }

        var imagesTensor = new Tensor(images.ToArray());
        var labelsTensor = new Tensor(labels.ToArray());

        // Créer le modèle
        var model = new Sequential();
        

        // Sauvegarder le modèle
        model.Save(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/models", id + ".h5"));
        

        // Mettre à jour le statut de l'IA
        var ia = await _context.IA.FindAsync(id);
        if (ia != null)
        {
            ia.trained_ia = true;
            await _context.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine($"IA {id} not found");
        }
        TrainingStatuses[id].IsCompleted = true;
    }

// Fonction pour charger et prétraiter les images (à compléter selon votre besoin)
    private float[] LoadImage(string path)
    {
        // Charger l'image depuis le chemin et la transformer en tableau de float normalisé (0-1)
        var image = PhysicalFile(path);
        image.Mutate(x => x.Resize(128, 128)); // Redimensionner si nécessaire
        var imageData = new float[128 * 128 * 3];
        
        // Convertir l'image en tableau de floats (normalisé)
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                var pixel = image[x, y];
                imageData[(y * 128 + x) * 3] = pixel.R / 255.0f;
                imageData[(y * 128 + x) * 3 + 1] = pixel.G / 255.0f;
                imageData[(y * 128 + x) * 3 + 2] = pixel.B / 255.0f;
            }
        }

        return imageData;
    }

}