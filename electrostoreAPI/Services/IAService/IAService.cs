using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using System.Text.Json;
using electrostore.Services.SessionService;
using electrostore.Services.FileService;

namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IFileService _fileService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _iaServiceUrl = "http://electrostoreIA:5000";
    private readonly string _modelsPath = "models";

    public IAService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IFileService fileService, IHttpClientFactory httpClientFactory)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _fileService = fileService;
        _httpClientFactory = httpClientFactory;
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
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        return _mapper.Map<ReadIADto>(ia);
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create IA");
        }
        var newIA = _mapper.Map<IA>(iaDto);
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
        var iaToUpdate = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        if (iaDto.nom_ia is not null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        // if model exists set trained_ia to true
        if (await _fileService.FileExists(GetModelFilePath(id)) && !iaToUpdate.trained_ia)
        {
            iaToUpdate.trained_ia = true;
        }
        else if (!await _fileService.FileExists(GetModelFilePath(id)) && iaToUpdate.trained_ia)
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
        var iaToDelete = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        // remove model if exists
        await _fileService.DeleteFile(GetModelFilePath(id));
        await _fileService.DeleteFile(GetModelItemListFilePath(id));
        _context.IA.Remove(iaToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<IAStatusDto> GetIATrainingStatusById(int id)
    {
        if (await _context.IA.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(_iaServiceUrl + "/status/" + id);
            var content = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? new Dictionary<string, JsonElement>();
            return new IAStatusDto
            {
                Status = GetStringValue(status, "status", "unknown"),
                Message = GetStringValue(status, "message", "unknown"),
                Epoch = GetIntValue(status, "epoch", 0),
                Accuracy = GetFloatValue(status, "accuracy", 0),
                ValAccuracy = GetFloatValue(status, "val_accuracy", 0),
                Loss = GetFloatValue(status, "loss", 0),
                ValLoss = GetFloatValue(status, "val_loss", 0)
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
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(_iaServiceUrl + "/train/" + id, null);
            // check if 200 OK
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Error while training IA");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new InvalidOperationException("Error while training IA", e); 
        }
    }

    public async Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        if (!ia.trained_ia)
        {
            throw new InvalidOperationException("IA is not trained");
        }
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            PredictionOutput newDetecResult;
            // requete POST avec l'image à scanner
            var response = await httpClient.PostAsync(_iaServiceUrl + "/detect/" + id,
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
                newDetecResult = new PredictionOutput
                {
                    PredictedLabel = -1,
                    Score = 0
                };
                return newDetecResult;
            }
            newDetecResult = new PredictionOutput
            {
                PredictedLabel = GetIntValue(json, "predicted_class", -1),
                Score = GetFloatValue(json, "confidence", 0)
            };
            return newDetecResult;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new InvalidOperationException("Error while detecting item", e);
        }
    }

    private static string GetModelFilePath(int id)
    {
        return "Model" + id.ToString() + ".keras";
    }
    private static string GetModelItemListFilePath(int id)
    {
        return "ItemList" + id.ToString() + ".txt";
    }

    private static string GetStringValue(Dictionary<string, JsonElement> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String 
            ? value.GetString()! 
            : defaultValue;
    }

    private static int GetIntValue(Dictionary<string, JsonElement> dict, string key, int defaultValue)
    {
        return dict.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number 
            ? value.GetInt32() 
            : defaultValue;
    }

    private static float GetFloatValue(Dictionary<string, JsonElement> dict, string key, float defaultValue)
    {
        return dict.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number 
            ? value.GetSingle() 
            : defaultValue;
    }
}