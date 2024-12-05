using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemDocumentService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/document")]

    public class ItemDocumentController : ControllerBase
    {
        private readonly IItemDocumentService _itemDocumentService;

        public ItemDocumentController(IItemDocumentService itemDocumentService)
        {
            _itemDocumentService = itemDocumentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadItemDocumentDto>>> GetItemsDocumentsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemsDocuments = await _itemDocumentService.GetItemsDocumentsByItemId(id_item, limit, offset);
            var CountList = await _itemDocumentService.GetItemsDocumentsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(itemsDocuments);
        }

        [HttpGet("{id_itemDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDocumentDto>> GetItemDocumentById([FromRoute] int id_itemDocument, [FromRoute] int id_item)
        {
            var itemDocument = await _itemDocumentService.GetItemDocumentById(id_itemDocument, id_item);
            return Ok(itemDocument);
        }

        [HttpGet("{id_itemDocument}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadItemDocument([FromRoute] int id_itemDocument, [FromRoute] int id_item)
        {
            var itemDocument = await _itemDocumentService.GetItemDocumentById(id_itemDocument, id_item);
            var result = await _itemDocumentService.GetFile(itemDocument.url_item_document); // check if itemDocument.url_item_document is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType, itemDocument.name_item_document);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDocumentDto>> CreateItemDocument([FromForm] CreateItemDocumentByItemDto itemDocumentDto, [FromRoute] int id_item)
        {
            var itemDocumentDtoFull = new CreateItemDocumentDto
            {
                id_item = id_item,
                name_item_document = itemDocumentDto.name_item_document,
                type_item_document = itemDocumentDto.type_item_document,
                document = itemDocumentDto.document
            };
            var itemDocument = await _itemDocumentService.CreateItemDocument(itemDocumentDtoFull);
            return CreatedAtAction(nameof(GetItemDocumentById), new { id_itemDocument = itemDocument.id_item_document, id_item = itemDocument.id_item }, itemDocument);
        }

        [HttpPut("{id_itemDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDocumentDto>> UpdateItemDocument([FromRoute] int id_itemDocument, [FromForm] UpdateItemDocumentDto itemDocumentDto, [FromRoute] int id_item)
        {
            var itemDocument = await _itemDocumentService.UpdateItemDocument(id_itemDocument, itemDocumentDto, id_item);
            return Ok(itemDocument);
        }

        [HttpDelete("{id_itemDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemDocument([FromRoute] int id_itemDocument, [FromRoute] int id_item)
        {
            await _itemDocumentService.DeleteItemDocument(id_itemDocument, id_item);
            return NoContent();
        }
    }
}