const CommandStatus = {
	Created: 0,
	Processing: 1,
	InTransit: 2,
	Delivered: 3,
	Cancelled: 4,
	Returned: 5,
	Failed: 6,
	Unknown: 7,
	Archived: 8,
};

export default CommandStatus;