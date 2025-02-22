using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.IAService;
using Swashbuckle.AspNetCore.Annotations;
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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadIADto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] string? idResearch = null)
        {
            var idList = string.IsNullOrWhiteSpace(idResearch) ? null : idResearch.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
            var ias = await _iaService.GetIA(limit, offset, idList);
            var CountList = await _iaService.GetIACount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(ias);
        }

        [HttpGet("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            return Ok(ia);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> GetTrainingStatus(int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("http://electrostoreia:5000/status/" + id_ia);
                var content = await response.Content.ReadAsStringAsync();
                // convert the response to a json object
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                return Ok(json);
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = e.Message });
            }
        }

        [HttpPost("{id_ia}/train")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<bool>> TrainIA([FromRoute] int id_ia)
        {
            if (User is not null)
            {
                if (!User.IsInRole("admin"))
                {
                    return Unauthorized(new { message = "You are not allowed to train an IA" });
                }
            }
            var ia = await _iaService.GetIAById(id_ia);
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://electrostoreia:5000/train/" + id_ia, null);
                var content = await response.Content.ReadAsStringAsync();
                // convert the response to a json object
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                return Ok(json);
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = e.Message });
            }
        }

        [HttpPost("{id_ia}/detect")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PredictionOutput>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var ia = await _iaService.GetIAById(id_ia);
            if (!ia.trained_ia)
            {
                return BadRequest(new { message = "The IA is not trained" });
            }
            try
            {
                var httpClient = new HttpClient();
                var newDetecResult = new PredictionOutput();
                // requete POST avec l'image à scanner
                var response = await httpClient.PostAsync("http://electrostoreia:5000/detect/" + id_ia,
                    new MultipartFormDataContent
                    {
                        { new StreamContent(img_to_scan.img_file.OpenReadStream()), "img_file", img_to_scan.img_file.FileName }
                    }
                );
                var content = await response.Content.ReadAsStringAsync();
                // convert the response to a json object
                var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                if (json is null)
                {
                    newDetecResult = new PredictionOutput {
                        PredictedLabel = -1,
                        Score = 0
                    };
                    return Ok(newDetecResult);
                }
                newDetecResult = new PredictionOutput {
                    PredictedLabel = json.TryGetValue("predicted_class", out var predicted_class) && predicted_class.ValueKind == JsonValueKind.Number ? predicted_class.GetInt32() : -1,
                    Score = json.TryGetValue("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number ? confidence.GetSingle() : 0
                };
                return Ok(newDetecResult);
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = e.Message });
            }
        }

        [HttpPut("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            return Ok(iaToUpdate);
        }

        [HttpDelete("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}