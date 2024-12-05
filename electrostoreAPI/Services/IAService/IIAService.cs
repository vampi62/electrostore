using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.IAService;

public interface IIAService
{
    Task<List<ReadIADto>> GetIA(int limit = 100, int offset = 0);

    Task<int> GetIACount();

    Task<ReadIADto> GetIAById(int id);

    Task<ReadIADto> CreateIA(CreateIADto IADto);

    Task<ReadIADto> UpdateIA(int id, UpdateIADto IADto);

    Task DeleteIA(int id);
/* 
    Task<TrainingStatus> GetTrainingStatus(string id);

    Task<ReadItemDto> DetectItem(int id, IFormFile imgToScan);

    Task<GetTrainStart> TrainIA(int id); */
}