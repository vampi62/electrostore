
namespace ElectrostoreAPI.Enums;

public enum TrackingStatus
{
    NotFound,
    InfoReceived,
    InTransit,
    Expired,
    AvailableForPickup,
    OutForDelivery,
    DeliveryFailure,
    Delivered,
    Exception,
    Unknown
}