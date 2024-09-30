using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.IAService;
using System.Net;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/ia")]

    public class IAController : ControllerBase
    {
        private readonly IIAService _iaService;

        public IAController(IIAService iaService)
        {
            _iaService = iaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadIADto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var ias = await _iaService.GetIA(limit, offset);
            return Ok(ias);
        }

        [HttpGet("{id_ia}")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            if (ia.Result is BadRequestObjectResult)
            {
                return ia.Result;
            }
            if (ia.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(ia.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        public async Task<IActionResult> GetTrainingStatus(int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            if (ia.Result is BadRequestObjectResult)
            {
                return ia.Result;
            }
            if (ia.Value == null)
            {
                return StatusCode(500);
            }
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreIA:5000/status/" + id_ia);
            var content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(content);
            } else {
                return Ok(content);
            }
        }

        [HttpPost("{id_ia}/train")]
        public async Task<ActionResult<bool>> TrainIA([FromRoute] int id_ia)
        {
            if (User != null)
            {
                if (!User.IsInRole("admin"))
                {
                    return Unauthorized(new { message = "You are not allowed to train an IA" });
                }
            }
            var ia = await _iaService.GetIAById(id_ia);
            if (ia.Result is BadRequestObjectResult)
            {
                return ia.Result;
            }
            if (ia.Value == null)
            {
                return StatusCode(500);
            }
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreIA:5000/train/" + id_ia);
            var content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(content);
            } else {
                return Ok(content);
            }
        }

        [HttpPost("{id_ia}/detect")]
        public async Task<ActionResult<ReadItemDto>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var ia = await _iaService.GetIAById(id_ia);
            if (ia.Result is BadRequestObjectResult)
            {
                return ia.Result;
            }
            if (ia.Value == null)
            {
                return StatusCode(500);
            }
            var httpClient = new HttpClient();
            // requete POST avec l'image à scanner
            var response = await httpClient.PostAsync("http://electrostoreIA:5000/detect/" + id_ia,
                new MultipartFormDataContent
                {
                    { new StreamContent(img_to_scan.img_file.OpenReadStream()), "img_file", img_to_scan.img_file.FileName }
                }
            );
            var content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(content);
            } else {
                return Ok(content);
            }
        }

        [HttpPut("{id_ia}")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            if (iaToUpdate.Result is BadRequestObjectResult)
            {
                return iaToUpdate.Result;
            }
            if (iaToUpdate.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(iaToUpdate.Value);
        }

        [HttpDelete("{id_ia}")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}