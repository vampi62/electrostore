using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using Grpc.Core;

namespace ElectrostoreAPI.Services.ImgService;

public interface IImgService
{
    public Task<PaginatedResponseDto<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadImgDto> GetImgById(int id, int? itemId = null);

    public Task<ReadImgDto> CreateImg(CreateImgDto imgDto);

    public Task<ReadImgDto> UpdateImg(int id, UpdateImgDto imgDto, int? itemId = null);

    public Task DeleteImg(int id, int? itemId = null);

    Task StreamTrainingImagesAsync(IAsyncStreamWriter<TrainingImage> responseStream, HashSet<string>? requestedSet, CancellationToken cancellationToken);
}