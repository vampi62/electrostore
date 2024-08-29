using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{id_img}")]
        public async Task<ActionResult<ReadIAImgDto>> GetIAImgById([FromRoute] int id_ia, [FromRoute] int id_img)
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
        
        [HttpPost]
        public async Task<ActionResult<ReadIAImgDto>> CreateIAImgs([FromRoute] int id_ia, [FromBody] int[] imgs)
        {
            var resultList = new List<ActionResult<ReadIAImgDto>>();
            for(int i = 0; i < imgs.Length; i++)
            {
                var iaImgDto = new CreateIAImgDto
                {
                    id_ia = id_ia,
                    id_img = imgs[i]
                };
                var iaImg = await _iaImgService.CreateIAImg(iaImgDto);
                if (iaImg.Result is BadRequestObjectResult || iaImg.Value == null)
                {
                    resultList.Add(iaImg.Result);
                }
                else
                {
                    resultList.Add(iaImg.Value);
                }
            }
            return Ok(resultList);
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
            if (iaImg.Result is BadRequestObjectResult)
            {
                return iaImg.Result;
            }
            if (iaImg.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetIAImgById), new { id_ia = iaImg.Value.id_ia, id_img = iaImg.Value.id_img }, iaImg.Value);
        }

        [HttpDelete("{id_img}")]
        public async Task<ActionResult> DeleteIAImg([FromRoute] int id_ia, [FromRoute] int id_img)
        {
            await _iaImgService.DeleteIAImg(id_ia, id_img);
            return NoContent();
        }
    }
}