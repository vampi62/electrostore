using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.CarrierService;

public class CarrierService : ICarrierService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private const string DemoModeKey = "DemoMode";

    public CarrierService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    // limit the number of carrier to 100 and add offset and search parameters
    public async Task<PaginatedResponseDto<ReadCarrierDto>> GetCarriers(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
        var query = _context.Carriers.AsQueryable();
        var filterResult = default(Expression<Func<Carriers, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_carrier));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Carriers>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Carriers>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_carrier", Order = "asc" };
                    query = query.OrderBy(c => c.id_carrier);
                }
            }
            else
            {
                query = query.OrderBy(c => c.id_carrier);
            }
        }
        query = query.Skip(offset).Take(limit);
        var carrier = await query.ToListAsync();
        return new PaginatedResponseDto<ReadCarrierDto>
        {
            data = _mapper.Map<IEnumerable<ReadCarrierDto>>(carrier),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Carriers.CountAsync(filterResult ?? (c => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Carriers.Skip(offset + limit).AnyAsync(filterResult ?? (c => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadCarrierDto> GetCarrierById(int id)
    {
        var carrier = await _context.Carriers.FindAsync(id) ?? throw new KeyNotFoundException($"Carrier with id '{id}' not found");
        return _mapper.Map<ReadCarrierDto>(carrier);
    }

    public async Task<ReadCarrierDto> CreateCarrier(CreateCarrierDto carrierDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a carrier");
        }
        var newCarrier = _mapper.Map<Carriers>(carrierDto);
        _context.Carriers.Add(newCarrier);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCarrierDto>(newCarrier);
    }

    public async Task<ReadCarrierDto> CreateFirstCarrier(CreateCarrierDto carrierDto)
    {
        var newCarrier = _mapper.Map<Carriers>(carrierDto);
        _context.Carriers.Add(newCarrier);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCarrierDto>(newCarrier);
    }

    public async Task<ReadCarrierDto> UpdateCarrier(int id, UpdateCarrierDto carrierDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a carrier");
        }
        var carrierToUpdate = await _context.Carriers.FindAsync(id) ?? throw new KeyNotFoundException($"Carrier with id '{id}' not found");
        if (carrierDto.country_carrier is not null)
        {
            carrierToUpdate.country_carrier = carrierDto.country_carrier.Value;
        }
        if (carrierDto.country_iso_carrier is not null)
        {
            carrierToUpdate.country_iso_carrier = carrierDto.country_iso_carrier;
        }
        if (carrierDto.email_carrier is not null)
        {
            carrierToUpdate.email_carrier = carrierDto.email_carrier;
        }
        if (carrierDto.tel_carrier is not null)
        {
            carrierToUpdate.tel_carrier = carrierDto.tel_carrier;
        }
        if (carrierDto.url_carrier is not null)
        {
            carrierToUpdate.url_carrier = carrierDto.url_carrier;
        }
        if (carrierDto.name_carrier is not null)
        {
            carrierToUpdate.name_carrier = carrierDto.name_carrier;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCarrierDto>(carrierToUpdate);
    }

    public async Task DeleteCarrier(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a carrier");
        }
        var carrierToDelete = await _context.Carriers.FindAsync(id) ?? throw new KeyNotFoundException($"Carrier with id '{id}' not found");
        _context.Carriers.Remove(carrierToDelete);
        await _context.SaveChangesAsync();
    }
}