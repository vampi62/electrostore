using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.IAService;

public interface IIAService
{
    Task<List<ReadIADto>> GetIA(int limit = 100, int offset = 0);

    Task<ActionResult<ReadIADto>> GetIAById(int id);

    Task<ReadIADto> CreateIA(CreateIADto IADto);

    Task<ActionResult<ReadIADto>> UpdateIA(int id, UpdateIADto IADto);

    Task<IActionResult> DeleteIA(int id);

    Task<ReadItemDto> DetectItem(int id, IFormFile newFile);

    Task<ActionResult<bool>> TrainIA(int id);
}