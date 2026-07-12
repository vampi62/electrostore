
const TrackingStatus = {
	NotFound: 0,
	InfoReceived: 1,
	InTransit: 2,
	Expired: 3,
	AvailableForPickup: 4,
	OutForDelivery: 5,
	DeliveryFailure: 6,
	Delivered: 7,
	Exception: 8,
	Unknown: 9,
};

export default TrackingStatus;