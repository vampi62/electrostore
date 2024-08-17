using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.IAImgService;

public interface IIAImgService
{
    public Task<ActionResult<IEnumerable<ReadIAImgDto>>> GetIAImgByIAId(int idIA, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadIAImgDto>>> GetIAImgByImgId(int idImg, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadIAImgDto>> GetIAImgById(int idIA, int idImg);

    public Task<ActionResult<ReadIAImgDto>> CreateIAImg(CreateIAImgDto IAImgDto);

    public Task<IActionResult> DeleteIAImg(int idIA, int idImg);
}