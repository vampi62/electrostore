import type { RSQLFilter, RSQLSort } from "@/types/rsql";

export interface FilterStrictMode {
	key: string;
	storeKey?: string;
	typeData?: string;
	disableLocalFilter?: boolean;
}

export interface FilterLabel {
	key: string;
	label: string;
	value?: any;
	type?: string;
	typeData?: string;
	compareMethod?: string;
	placeholder?: string;
	class?: string;
	options?: Record<string, any>;
	sortOptions?: string;
	fetchOptions?: (limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => any;
	storeData?: any;
	storeKey?: string;
	strictMode?: FilterStrictMode;
	preset?: any;
	showCondition?: string;
	enableCondition?: string;
	disableLocalFilter?: boolean;
	// used by type: "checkbox" to translate the checked state into the filtered value
	valueIfTrue?: any;
	valueIfFalse?: any;
	// set at runtime by FilterContainer when the user switches a strictMode filter on/off
	strictModeEnabled?: boolean;
	_originalKey?: string;
	_originalCompareMethod?: string;
	_originalTypeData?: string;
	_disableLocalFilter?: boolean;
}
