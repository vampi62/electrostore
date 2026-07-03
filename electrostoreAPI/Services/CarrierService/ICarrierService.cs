using ElectrostoreAPI.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ElectrostoreAPI.Services.CarrierService;

public interface ICarrierService
{
    public Task<PaginatedResponseDto<ReadCarrierDto>> GetCarriers(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null);

    public Task<ReadCarrierDto> GetCarrierById(int id);

    public Task<ReadCarrierDto> CreateCarrier(CreateCarrierDto carrierDto);

    public Task<ReadCarrierDto> CreateFirstCarrier(CreateCarrierDto carrierDto);

    public Task<ReadCarrierDto> UpdateCarrier(int id, UpdateCarrierDto carrierDto);

    public Task DeleteCarrier(int id);
}