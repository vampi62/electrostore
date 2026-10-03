const CronJobAction = {
	PackageTracking: 0,
	StockLowAlert: 1,
	WeeklyItemMovementReport: 2,
} as const;

export default CronJobAction;
