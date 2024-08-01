using electrostore.Dto;

namespace electrostore.Services.ImgService;

public interface IImgService
{
    public Task<IEnumerable<ReadImgDto>> GetImgs(int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ReadImgDto> GetImgById(int id, int? itemId = null);

    public Task<ReadImgDto> CreateImg(CreateImgDto ImgDto);

    public Task<ReadImgDto> UpdateImg(int id, UpdateImgDto ImgDto, int? itemId = null);

    public Task DeleteImg(int id, int? itemId = null);
}