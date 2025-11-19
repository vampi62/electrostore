using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ValidateStoreService;

public interface IValidateStoreService
{
    public void ValidateLedPosition(Leds led, Stores store);

    public void ValidateBoxPosition(Boxs box, Stores store);

    public Task UpdateStoreInformations(Stores storeToUpdate, UpdateStoreDto storeDto);

    public Task UpdateBoxInformations(Boxs boxToUpdate, UpdateBoxDto boxDto);

    public Task UpdateLedInformations(Leds ledToUpdate, UpdateLedDto ledDto);

    public Task CheckUpdateStoreOutsideElement(Stores storeToUpdate);

    public Task CheckCreateBoxPositionOverlap(CreateBoxDto newBox);

    public Task CheckUpdateBoxPositionOverlap(Boxs boxToUpdate);
}