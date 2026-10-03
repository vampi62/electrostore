const EquipementStatus = {
	Operational: 0,
	InMaintenance: 1,
	OutOfService: 2,
	Retired: 3,
} as const;

export default EquipementStatus;
