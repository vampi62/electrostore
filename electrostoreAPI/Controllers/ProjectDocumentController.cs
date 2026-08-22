using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjectDocumentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/document")]

    public class ProjectDocumentController : ControllerBase
    {
        private readonly IProjectDocumentService _projectDocumentService;
        private readonly IFileService _fileService;

        public ProjectDocumentController(IProjectDocumentService projectDocumentService, IFileService fileService)
        {
            _projectDocumentService = projectDocumentService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadProjectDocumentDto>>> GetProjectsDocumentsByProjectId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project_document=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_project_document,asc' or 'name_project_document,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectsDocuments = await _projectDocumentService.GetProjectDocumentsByProjectId(id_project, limit, offset, rsqlDto, sortDto);
            return Ok(projectsDocuments);
        }

        [HttpGet("{id_project_document}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectDocumentDto>> GetProjectDocumentById([FromRoute] int id_project_document, [FromRoute] int id_project)
        {
            var projectDocument = await _projectDocumentService.GetProjectDocumentById(id_project_document, id_project);
            return Ok(projectDocument);
        }
        
        [HttpGet("{id_project_document}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadProjectDocument([FromRoute] int id_project_document, [FromRoute] int id_project)
        {
            var projectDocument = await _projectDocumentService.GetProjectDocumentById(id_project_document, id_project);
            var result = await _fileService.GetFile(projectDocument.url_project_document);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type, projectDocument.name_project_document);
            }
            else
            {
                return NotFound(result.error_message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectDocumentDto>> CreateProjectDocument([FromForm] CreateProjectDocumentByProjectDto projectDocumentDto, [FromRoute] int id_project)
        {
            var projectDocumentDtoFull = new CreateProjectDocumentDto
            {
                id_project = id_project,
                name_project_document = projectDocumentDto.name_project_document,
                document = projectDocumentDto.document
            };
            var projectDocument = await _projectDocumentService.CreateProjectDocument(projectDocumentDtoFull);
            return CreatedAtAction(nameof(GetProjectDocumentById), new { id_project_document = projectDocument.id_project_document, id_project = projectDocument.id_project }, projectDocument);
        }

        [HttpPut("{id_project_document}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectDocumentDto>> UpdateProjectDocument([FromRoute] int id_project_document, [FromBody] UpdateProjectDocumentDto projectDocumentDto, [FromRoute] int id_project)
        {
            var projectDocument = await _projectDocumentService.UpdateProjectDocument(id_project_document, projectDocumentDto, id_project);
            return Ok(projectDocument);
        }

        [HttpDelete("{id_project_document}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectDocument([FromRoute] int id_project_document, [FromRoute] int id_project)
        {
            await _projectDocumentService.DeleteProjectDocument(id_project_document, id_project);
            return NoContent();
        }
    }
}