using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.IAImgService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/ia/{id_ia}/img")]

    public class IAImgController : ControllerBase
    {
        private readonly IIAImgService _iaImgService;

        public IAImgController(IIAImgService iaImgService)
        {
            _iaImgService = iaImgService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadIAImgDto>>> GetIAImgByIAId([FromRoute] int id_ia, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var iaImgs = await _iaImgService.GetIAImgByIAId(id_ia, limit, offset);
            return Ok(iaImgs);
        }

        [HttpGet("{id_img}")]
        public async Task<ActionResult<ReadIAImgDto>> GetIAImgById([FromRoute] int id_ia, [FromRoute] int id_img)
        {
            var iaImg = await _iaImgService.GetIAImgById(id_ia, id_img);
            return Ok(iaImg);
        }

        [HttpPost("{id_img}")]
        public async Task<ActionResult<ReadIAImgDto>> CreateIAImg([FromRoute] int id_ia, [FromRoute] int id_img)
        {
            var iaImgDto = new CreateIAImgDto
            {
                id_ia = id_ia,
                id_img = id_img
            };
            var iaImg = await _iaImgService.CreateIAImg(iaImgDto);
            return CreatedAtAction(nameof(GetIAImgById), new { id_ia = iaImg.id_ia, id_img = iaImg.id_img }, iaImg);
        }

        [HttpDelete("{id_img}")]
        public async Task<ActionResult> DeleteIAImg([FromRoute] int id_ia, [FromRoute] int id_img)
        {
            await _iaImgService.DeleteIAImg(id_ia, id_img);
            return NoContent();
        }
    }
}