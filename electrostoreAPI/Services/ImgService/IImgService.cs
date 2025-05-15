using electrostore.Dto;

namespace electrostore.Services.ImgService;

public interface IImgService
{
    public Task<IEnumerable<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<int> GetImgsCountByItemId(int itemId);

    public Task<ReadImgDto> GetImgById(int id, int? itemId = null);

    public Task<ReadImgDto> CreateImg(CreateImgDto imgDto);

    public Task<ReadImgDto> UpdateImg(int id, UpdateImgDto imgDto, int? itemId = null);

    public Task DeleteImg(int id, int? itemId = null);

    public Task<GetFileResult> GetImageFile(string url);
}