using electrostore.Dto;

namespace electrostore.Services.IAImgService;

public interface IIAImgService
{
    public Task<IEnumerable<ReadIAImgDto>> GetIAImgByIAId(int idIA, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadIAImgDto>> GetIAImgByImgId(int idImg, int limit = 100, int offset = 0);

    public Task<ReadIAImgDto> GetIAImgById(int idIA, int idImg);

    public Task<ReadIAImgDto> CreateIAImg(CreateIAImgDto IAImgDto);

    public Task DeleteIAImg(int idIA, int idImg);
}