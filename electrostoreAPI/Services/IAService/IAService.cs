using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using System.Text.Json;
using electrostore.Services.SessionService;

namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public IAService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<IEnumerable<ReadIADto>> GetIA(int limit = 100, int offset = 0, List<int>? idResearch = null)
    {
        var query = _context.IA.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(ia => idResearch.Contains(ia.id_ia));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(ia => ia.id_ia);
        var ia = await query.ToListAsync();
        return _mapper.Map<List<ReadIADto>>(ia);
    }

    public async Task<int> GetIACount()
    {
        return await _context.IA.CountAsync();
    }

    public async Task<ReadIADto> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        return _mapper.Map<ReadIADto>(ia);
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create IA");
        }
        var newIA = new IA
        {
            nom_ia = iaDto.nom_ia,
            description_ia = iaDto.description_ia
        };
        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(newIA);
    }

    public async Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to update IA");
        }
        var iaToUpdate = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        if (iaDto.nom_ia is not null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        // if model exists set trained_ia to true
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && !iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = true;
        }
        else if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models","Model" + id.ToString() + ".keras")) && iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = false;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(iaToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete IA");
        }
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

    public async Task<IAStatusDto> GetIATrainingStatusById(int id)
    {
        if (await _context.IA.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"IA with id {id} not found");
        }
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreIA:5000/status/" + id);
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? throw new Exception("Error while getting IA training status");
            return new IAStatusDto
            {
                Status = status.TryGetValue("status", out var statusValue) && statusValue.ValueKind == JsonValueKind.String ? statusValue.GetString()! : "unknown",
                Message = status.TryGetValue("message", out var messageValue) && messageValue.ValueKind == JsonValueKind.String ? messageValue.GetString()! : "unknown",
                Epoch = status.TryGetValue("epoch", out var epochValue) && epochValue.ValueKind == JsonValueKind.Number ? epochValue.GetInt32() : 0,
                Accuracy = status.TryGetValue("accuracy", out var accuracyValue) && accuracyValue.ValueKind == JsonValueKind.Number ? accuracyValue.GetSingle() : 0,
                ValAccuracy = status.TryGetValue("val_accuracy", out var val_accuracyValue) && val_accuracyValue.ValueKind == JsonValueKind.Number ? val_accuracyValue.GetSingle() : 0,
                Loss = status.TryGetValue("loss", out var lossValue) && lossValue.ValueKind == JsonValueKind.Number ? lossValue.GetSingle() : 0,
                ValLoss = status.TryGetValue("val_loss", out var val_lossValue) && val_lossValue.ValueKind == JsonValueKind.Number ? val_lossValue.GetSingle() : 0
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new IAStatusDto
            {
                Status = "unknown",
                Message = "unknown",
                Epoch = 0,
                Accuracy = 0,
                ValAccuracy = 0,
                Loss = 0,
                ValLoss = 0
            };
        }
    }

    public async Task StartIATrainById(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to train IA");
        }
        if (await _context.IA.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"IA with id {id} not found");
        }
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://electrostoreIA:5000/train/" + id, null);
            // check if 200 OK
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Error while training IA");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception("Error while training IA", e);
        }
    }

    public async Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id {id} not found");
        if (!ia.trained_ia)
        {
            throw new Exception("IA is not trained");
        }
        try
        {
            var httpClient = new HttpClient();
            PredictionOutput newDetecResult;
            // requete POST avec l'image à scanner
            var response = await httpClient.PostAsync("http://electrostoreIA:5000/detect/" + id,
                new MultipartFormDataContent
                {
                    { new StreamContent(detecDto.img_file.OpenReadStream()), "img_file", detecDto.img_file.FileName }
                }
            );
            var content = await response.Content.ReadAsStringAsync();
            // convert the response to a json object
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            if (json is null)
            {
                newDetecResult = new PredictionOutput {
                    PredictedLabel = -1,
                    Score = 0
                };
                return newDetecResult;
            }
            newDetecResult = new PredictionOutput {
                PredictedLabel = json.TryGetValue("predicted_class", out var predicted_class) && predicted_class.ValueKind == JsonValueKind.Number ? predicted_class.GetInt32() : -1,
                Score = json.TryGetValue("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number ? confidence.GetSingle() : 0
            };
            return newDetecResult;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception("Error while detecting item", e);
        }
    }
}