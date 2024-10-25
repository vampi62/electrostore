using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.IAService;
using System.Net;
using System.Text.Json;

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
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<IEnumerable<ReadIADto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var ias = await _iaService.GetIA(limit, offset);
            return Ok(ias);
        }

        [HttpGet("{id_ia}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            return Ok(ia);
        }

        [HttpPost]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<IActionResult> GetTrainingStatus(int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreia:5000/status/" + id_ia);
            var content = await response.Content.ReadAsStringAsync();
            // convert the response to a json object
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(json);
            }
            else
            {
                return Ok(json);
            }
        }

        [HttpPost("{id_ia}/train")]
        [Authorize(Policy = "AccessTokenPolicy")]
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
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://electrostoreia:5000/train/" + id_ia);
            var content = await response.Content.ReadAsStringAsync();
            // convert the response to a json object
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(json);
            }
            else
            {
                return Ok(json);
            }
        }

        [HttpPost("{id_ia}/detect")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadItemDto>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var ia = await _iaService.GetIAById(id_ia);
            var httpClient = new HttpClient();
            // requete POST avec l'image à scanner
            var response = await httpClient.PostAsync("http://electrostoreia:5000/detect/" + id_ia,
                new MultipartFormDataContent
                {
                    { new StreamContent(img_to_scan.img_file.OpenReadStream()), "img_file", img_to_scan.img_file.FileName }
                }
            );
            var content = await response.Content.ReadAsStringAsync();
            // convert the response to a json object
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return BadRequest(json);
            }
            else
            {
                return Ok(json);
            }
        }

        [HttpPut("{id_ia}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            return Ok(iaToUpdate);
        }

        [HttpDelete("{id_ia}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}