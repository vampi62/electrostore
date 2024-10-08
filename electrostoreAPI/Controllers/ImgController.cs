using Microsoft.AspNetCore.Mvc;
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
            if (img.Result is BadRequestObjectResult)
            {
                return img.Result;
            }
            if (img.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(img.Value);
        }

        [HttpGet("{id_img}/show")]
        public async Task<ActionResult<ReadImgDto>> GetImgData([FromRoute] int id_img)
        {
            var img = await _imgService.GetImgById(id_img);
            if (img.Result is BadRequestObjectResult)
            {
                return img.Result;
            }
            if (img.Value == null)
            {
                return StatusCode(500);
            }
            var result = await _imgService.GetImageFile(img.Value.url_img); // check if img.url_img is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpPost]
        public async Task<ActionResult<ReadImgDto>> CreateImg([FromForm] CreateImgDto imgDto) // 5MB max
        {
            var img = await _imgService.CreateImg(imgDto);
            if (img.Result is BadRequestObjectResult)
            {
                return img.Result;
            }
            if (img.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetImgById), new { id_img = img.Value.id_img }, img.Value);
        }

        [HttpPut("{id_img}")]
        public async Task<ActionResult<ReadImgDto>> UpdateImg([FromRoute] int id_img, [FromBody] UpdateImgDto imgDto)
        {
            var img = await _imgService.UpdateImg(id_img, imgDto);
            if (img.Result is BadRequestObjectResult)
            {
                return img.Result;
            }
            if (img.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetImgById), new { id_img = img.Value.id_img }, img.Value);
        }

        [HttpDelete("{id_img}")]
        public async Task<ActionResult> DeleteImg([FromRoute] int id_img)
        {
            await _imgService.DeleteImg(id_img);
            return Ok();
        }
    }
}