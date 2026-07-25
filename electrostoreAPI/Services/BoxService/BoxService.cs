using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ValidateStoreService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.BoxService;

public class BoxService : IBoxService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IValidateStoreService _validateStoreService;
    private readonly ILogger<BoxService> _logger;

    public BoxService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IValidateStoreService validateStoreService, ILogger<BoxService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _validateStoreService = validateStoreService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadExtendedBoxDto>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetBoxsByStoreId: storeId {StoreId}, limit {Limit}, offset {Offset}", storeId, limit, offset);
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            _logger.LogWarning("GetBoxsByStoreId: store {StoreId} not found", storeId);
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        var query = _context.Boxs.AsQueryable();
        var filterResult = default(Expression<Func<Boxs, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_store", SearchType = "eq", Value = storeId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Boxs>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<Boxs>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_box", Order = "asc" };
                query = query.OrderBy(b => b.id_box);
            }
        }
        else
        {
            query = query.OrderBy(b => b.id_box);
        }
        query = query.Skip(offset).Take(limit);
        var box = await query
            .Select(b => new
            {
                Box = b,
                BoxsTagsCount = b.BoxsTags.Count,
                ItemsBoxsCount = b.ItemsBoxs.Count,
                Store = expand != null && expand.Contains("store") ? b.Store : null,
                BoxsTags = expand != null && expand.Contains("box_tags") ? b.BoxsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? b.ItemsBoxs.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedBoxDto>
        {
            data = box.Select(b => _mapper.Map<ReadExtendedBoxDto>(b.Box) with
            {
                box_tags_count = b.BoxsTagsCount,
                item_boxs_count = b.ItemsBoxsCount,
                store = _mapper.Map<ReadStoreDto>(b.Store),
                box_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(b.BoxsTags),
                item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(b.ItemsBoxs)
            }),
            pagination = new PaginationDto
            {
                total = await _context.Boxs.CountAsync(filterResult ?? (b => b.id_store == storeId)),
                nextOffset = offset + limit,
                hasMore = await _context.Boxs.Skip(offset + limit).AnyAsync(filterResult ?? (b => b.id_store == storeId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedBoxDto> GetBoxById(int id, int? storeId = null, List<string>? expand = null)
    {
        var query = _context.Boxs.AsQueryable();
        query = query.Where(b => b.id_box == id && (storeId == null || b.id_store == storeId));
        var box = await query
            .Select(b => new
            {
                Box = b,
                BoxsTagsCount = b.BoxsTags.Count,
                ItemsBoxsCount = b.ItemsBoxs.Count,
                Store = expand != null && expand.Contains("store") ? b.Store : null,
                BoxsTags = expand != null && expand.Contains("box_tags") ? b.BoxsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? b.ItemsBoxs.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync();
        if (box is null)
        {
            _logger.LogWarning("GetBoxById: box {BoxId} not found (storeId {StoreId})", id, storeId);
            throw new KeyNotFoundException($"Box with id '{id}' not found");
        }
        return _mapper.Map<ReadExtendedBoxDto>(box.Box) with
        {
            box_tags_count = box.BoxsTagsCount,
            item_boxs_count = box.ItemsBoxsCount,
            store = _mapper.Map<ReadStoreDto>(box.Store),
            box_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(box.BoxsTags),
            item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(box.ItemsBoxs)
        };
    }

    public async Task<ReadBoxDto> CreateBox(CreateBoxDto boxDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateBox: unauthorized attempt to create a box (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to create a box");
        }
        // check if the store exists
        var store = await _context.Stores.FindAsync(boxDto.id_store);
        if (store is null)
        {
            _logger.LogWarning("CreateBox: store {StoreId} not found", boxDto.id_store);
            throw new KeyNotFoundException($"Store with id '{boxDto.id_store}' not found");
        }
        await _validateStoreService.CheckCreateBoxPositionOverlap(boxDto);
        var newBox = _mapper.Map<Boxs>(boxDto);
        _validateStoreService.ValidateBoxPosition(newBox, store);
        _context.Boxs.Add(newBox);
        await _context.SaveChangesAsync();
        _logger.LogInformation("CreateBox: box {BoxId} created in store {StoreId}", newBox.id_box, newBox.id_store);
        return _mapper.Map<ReadBoxDto>(newBox);
    }

    public async Task<ReadBulkBoxDto> CreateBulkBox(List<CreateBoxDto> boxsDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateBulkBox: unauthorized attempt to create boxes (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to create boxs");
        }
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxDto in boxsDto)
        {
            try
            {
                // check if the store exists
                var store = await _context.Stores.FindAsync(boxDto.id_store);
                if (store is null)
                {
                    _logger.LogWarning("CreateBulkBox: store {StoreId} not found", boxDto.id_store);
                    throw new KeyNotFoundException($"Store with id '{boxDto.id_store}' not found");
                }
                await _validateStoreService.CheckCreateBoxPositionOverlap(boxDto);
                var newBox = _mapper.Map<Boxs>(boxDto);
                _validateStoreService.ValidateBoxPosition(newBox, store);
                _context.Boxs.Add(newBox);
                validQuery.Add(_mapper.Map<ReadBoxDto>(newBox));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxDto
                });
            }
        }
        if (errorQuery.Count == 0)
        {
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("CreateBulkBox: {ValidCount} boxes created, {ErrorCount} errors", validQuery.Count, errorQuery.Count);
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadBoxDto> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("UpdateBox: unauthorized attempt to update box {BoxId} (role {ClientRole})", id, clientRole);
            throw new UnauthorizedAccessException("You are not authorized to update a box");
        }
        var boxToUpdate = await _context.Boxs.FindAsync(id);
        if ((boxToUpdate is null) || (storeId is not null && boxToUpdate.id_store != storeId))
        {
            _logger.LogWarning("UpdateBox: box {BoxId} not found (storeId {StoreId})", id, storeId);
            throw new KeyNotFoundException($"Box with id '{id}' not found");
        }
        await _validateStoreService.UpdateBoxInformations(boxToUpdate, boxDto);
        var store = await _context.Stores.FindAsync(boxToUpdate.id_store);
        if (store is null)
        {
            _logger.LogWarning("UpdateBox: store {StoreId} not found", boxToUpdate.id_store);
            throw new KeyNotFoundException($"Store with id '{boxToUpdate.id_store}' not found");
        }
        _validateStoreService.ValidateBoxPosition(boxToUpdate, store);
        await _validateStoreService.CheckUpdateBoxPositionOverlap(boxToUpdate);
        await _context.SaveChangesAsync();
        _logger.LogInformation("UpdateBox: box {BoxId} updated", id);
        return _mapper.Map<ReadBoxDto>(boxToUpdate);
    }

    public async Task<ReadBulkBoxDto> UpdateBulkBox(List<UpdateBulkBoxByStoreDto> boxsDto, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("UpdateBulkBox: unauthorized attempt to update boxes (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to update boxes");
        }
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxDto in boxsDto)
        {
            try
            {
                var boxToUpdate = await _context.Boxs.FindAsync(boxDto.id_box);
                if ((boxToUpdate is null) || (storeId is not null && boxToUpdate.id_store != storeId))
                {
                    _logger.LogWarning("UpdateBulkBox: box {BoxId} not found (storeId {StoreId})", boxDto.id_box, storeId);
                    throw new KeyNotFoundException($"Box with id '{boxDto.id_box}' not found");
                }
                await _validateStoreService.UpdateBoxInformations(boxToUpdate, _mapper.Map<UpdateBoxDto>(boxDto));
                // check if the box XY position is not bigger than the store XY length
                var store = await _context.Stores.FindAsync(boxToUpdate.id_store);
                if (store is null)
                {
                    _logger.LogWarning("UpdateBulkBox: store {StoreId} not found", boxToUpdate.id_store);
                    throw new KeyNotFoundException($"Store with id '{boxToUpdate.id_store}' not found");
                }
                _validateStoreService.ValidateBoxPosition(boxToUpdate, store);
                validQuery.Add(_mapper.Map<ReadBoxDto>(boxToUpdate));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxDto
                });
            }
        }
        // if there is no error, 1: check if no box as the same XY position in the same store, 2: save changes if still no error
        if (errorQuery.Count == 0)
        {
            foreach (var boxDto in boxsDto)
            {
                try
                {
                    var boxToUpdate = await _context.Boxs.FindAsync(boxDto.id_box);
                    if (boxToUpdate is null)
                    {
                        _logger.LogWarning("UpdateBulkBox: box {BoxId} not found", boxDto.id_box);
                        throw new KeyNotFoundException($"Box with id '{boxDto.id_box}' not found");
                    }
                    await _validateStoreService.CheckUpdateBoxPositionOverlap(boxToUpdate);
                }
                catch (Exception e)
                {
                    errorQuery.Add(new ErrorDetail
                    {
                        Reason = e.Message,
                        Data = boxDto
                    });
                }
            }
        }
        if (errorQuery.Count == 0)
        {
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("UpdateBulkBox: {ValidCount} boxes updated, {ErrorCount} errors", validQuery.Count, errorQuery.Count);
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteBox(int id, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBox: unauthorized attempt to delete box {BoxId} (role {ClientRole})", id, clientRole);
            throw new UnauthorizedAccessException("You are not authorized to delete a box");
        }
        var boxToDelete = await _context.Boxs.FindAsync(id);
        if ((boxToDelete is null) || (storeId is not null && boxToDelete.id_store != storeId))
        {
            _logger.LogWarning("DeleteBox: box {BoxId} not found (storeId {StoreId})", id, storeId);
            throw new KeyNotFoundException($"Box with id '{id}' not found");
        }
        // check if the box has a item in it (ItemsBoxs) with qte_item_box > 0
        if (await _context.ItemsBoxs.AnyAsync(ib => ib.id_box == id && ib.qte_item_box > 0))
        {
            _logger.LogWarning("DeleteBox: box {BoxId} has items in it", id);
            throw new InvalidOperationException($"Box with id '{id}' has items in it");
        }
        _context.Boxs.Remove(boxToDelete);
        await _context.SaveChangesAsync();
        _logger.LogInformation("DeleteBox: box {BoxId} deleted", id);
    }

    public async Task<ReadBulkBoxDto> DeleteBulkBox(List<int> ids, int storeId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBulkBox: unauthorized attempt to delete boxes (role {ClientRole})", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to delete boxes");
        }
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var id in ids)
        {
            try
            {
                await DeleteBox(id, storeId);
                validQuery.Add(new ReadBoxDto
                {
                    id_box = id
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = id
                });
            }
        }
        _logger.LogInformation("DeleteBulkBox: {ValidCount} boxes deleted, {ErrorCount} errors", validQuery.Count, errorQuery.Count);
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}