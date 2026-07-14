
namespace ElectrostoreAPI.Enums;

public enum TrackingSubStatus
{
    NotFound_Other,
    NotFound_InvalidCode,
    InfoReceived,
    InTransit_PickedUp,
    InTransit_Other,
    InTransit_Departure,
    InTransit_Arrival,
    InTransit_CustomsProcessing,
    InTransit_CustomsReleased,
    InTransit_CustomsRequiringInformation,
    Expired_Other,
    AvailableForPickup_Other,
    OutForDelivery_Other,
    DeliveryFailure_Other,
    DeliveryFailure_NoBody,
    DeliveryFailure_Security,
    DeliveryFailure_Rejected,
    DeliveryFailure_InvalidAddress,
    Delivered_Other,
    Exception_Other,
    Exception_Returning,
    Exception_Returned,
    Exception_NoBody,
    Exception_Security,
    Exception_Damage,
    Exception_Rejected,
    Exception_Delayed,
    Exception_Lost,
    Exception_Destroyed,
    Exception_Cancel
}