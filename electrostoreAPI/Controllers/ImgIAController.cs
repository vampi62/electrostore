using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.IAImgService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/img/{id_img}/ia")]

    public class ImgIAController : ControllerBase
    {
        private readonly IIAImgService _iaImgService;

        public ImgIAController(IIAImgService iaImgService)
        {
            _iaImgService = iaImgService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadIAImgDto>>> GetIAImgByImgId([FromRoute] int id_img, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var iaImgs = await _iaImgService.GetIAImgByImgId(id_img, limit, offset);
            if (iaImgs.Result is BadRequestObjectResult)
            {
                return iaImgs.Result;
            }
            if (iaImgs.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(iaImgs.Value);
        }

        [HttpGet("{id_ia}")]
        public async Task<ActionResult<ReadIAImgDto>> GetIAImgById([FromRoute] int id_img,[FromRoute]  int id_ia)
        {
            var iaImg = await _iaImgService.GetIAImgById(id_ia, id_img);
            if (iaImg.Result is BadRequestObjectResult)
            {
                return iaImg.Result;
            }
            if (iaImg.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(iaImg.Value);
        }

        [HttpPost("{id_ia}")]
        public async Task<ActionResult<ReadIAImgDto>> CreateIAImg([FromRoute] int id_img, [FromRoute] int id_ia)
        {
            var iaImgDto = new CreateIAImgDto
            {
                id_img = id_img,
                id_ia = id_ia
            };
            var iaImg = await _iaImgService.CreateIAImg(iaImgDto);
            if (iaImg.Result is BadRequestObjectResult)
            {
                return iaImg.Result;
            }
            if (iaImg.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetIAImgById), new { id_img = iaImg.Value.id_img, id_ia = iaImg.Value.id_ia }, iaImg.Value);
        }

        [HttpDelete("{id_ia}")]
        public async Task<ActionResult> DeleteIAImg([FromRoute] int id_img, [FromRoute] int id_ia)
        {
            await _iaImgService.DeleteIAImg(id_ia, id_img);
            return NoContent();
        }
    }
}