using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ImgService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/img")]

    public class ImgController : ControllerBase
    {
        private readonly IImgService _imgService;

        public ImgController(IImgService imgService)
        {
            _imgService = imgService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadImgDto>>> GetImgs([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var imgs = await _imgService.GetImgs(limit, offset);
            return Ok(imgs);
        }

        [HttpGet("{id_img}")]
        public async Task<ActionResult<ReadImgDto>> GetImgById([FromRoute] int id_img)
        {
            var img = await _imgService.GetImgById(id_img);
            return Ok(img);
        }

        [HttpPost]
        public async Task<ActionResult<ReadImgDto>> CreateImg([FromBody] CreateImgDto imgDto)
        {
            var img = await _imgService.CreateImg(imgDto);
            return CreatedAtAction(nameof(GetImgById), new { id_img = img.id_img }, img);
        }

        [HttpPut("{id_img}")]
        public async Task<ActionResult<ReadImgDto>> UpdateImg([FromRoute] int id_img, [FromBody] UpdateImgDto imgDto)
        {
            var img = await _imgService.UpdateImg(id_img, imgDto);
            return CreatedAtAction(nameof(GetImgById), new { id_img = img.id_img }, img);
        }

        [HttpDelete("{id_img}")]
        public async Task<ActionResult> DeleteImg([FromRoute] int id_img)
        {
            await _imgService.DeleteImg(id_img);
            return Ok();
        }
    }
}