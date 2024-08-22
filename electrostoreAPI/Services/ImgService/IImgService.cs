using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ImgService;

public interface IImgService
{
    public Task<IEnumerable<ReadImgDto>> GetImgs(int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadImgDto>>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadImgDto>> GetImgById(int id, int? itemId = null);

    public Task<ActionResult<ReadImgDto>> CreateImg(CreateImgDto ImgDto);

    public Task<ActionResult<ReadImgDto>> UpdateImg(int id, UpdateImgDto ImgDto, int? itemId = null);

    public Task<IActionResult> DeleteImg(int id, int? itemId = null);

    public Task<GetImageFileResult> GetImageFile(string url);
}