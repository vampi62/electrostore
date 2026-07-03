
namespace ElectrostoreAPI.Enums;

public enum CommandStatus
{
    Created,
    Processing,
    InTransit,
    Delivered,
    Cancelled,
    Returned,
    Failed,
    Unknown,
    Archived
}