export interface FormLabel {
	// key in the storeData for the field
	key: string;
	// translation key for the label
	label: string;
	text?: string;
	// input type, e.g. 'text', 'number', 'select', 'checkbox', 'password', 'textarea', 'computed', 'custom'
	type?: string;
	// JavaScript expressions evaluated to enable / show the field
	enableCondition?: string;
	showCondition?: string;
	typeData?: string;
	placeholder?: string;
	value?: any;
	loading?: boolean;
	// select inputs
	options?: Record<string, any>;
	sort?: "asc" | "desc";
	// textarea inputs
	rows?: number;
	// type: "fetch-select"
	fetchStore?: Record<string, any>;
	fetchStoreKey?: string;
	fetchValueKey?: string;
	fetchFunction?: (...args: any[]) => any;
}
