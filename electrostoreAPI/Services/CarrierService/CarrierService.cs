using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.CarrierService;

public class CarrierService : ICarrierService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

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
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_carrier));
            rsql = null;
            sort = null;
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_carrier", order = "asc" })
            .ToResponseAsync(carrier => _mapper.Map<IEnumerable<ReadCarrierDto>>(carrier));
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