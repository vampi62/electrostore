import type { StateTree } from "pinia";
import type { FetchFunction } from "@/types/fetch";
import type { RSQLFilter } from "@/types/rsql";

// value of a filter, as it is sent to the api
export type FilterValue = RSQLFilter["value"];

// option of a select / datalist filter
export interface FilterOption {
	id: string | number;
	value: string;
}

export interface FilterStrictMode {
	key: string;
	storeKey?: string;
	typeData?: string;
	disableLocalFilter?: boolean;
}

// a filter is sent to the api as it is, so it extends RSQLFilter (key, compareMethod, value)
export interface FilterLabel extends RSQLFilter {
	// translation key for the label
	label: string;
	// input type, e.g. 'text', 'number', 'date', 'select', 'datalist', 'checkbox', 'hidden'
	type: string;
	typeData?: string;
	placeholder?: string;
	class?: string;
	// options of a select / datalist: { [id]: label }
	options?: Record<string | number, string>;
	sortOptions?: "asc" | "desc";
	fetchOptions?: FetchFunction;
	// store whose data are the options of a datalist, storeKey is the field of an element to display
	storeData?: StateTree;
	storeKey?: string;
	strictMode?: FilterStrictMode;
	preset?: FilterValue;
	// JavaScript expressions evaluated to show / enable the filter
	showCondition?: string;
	enableCondition?: string;
	disableLocalFilter?: boolean;
	// used by type: "checkbox" to translate the checked state into the filtered value
	valueIfTrue?: FilterValue;
	valueIfFalse?: FilterValue;
	// set at runtime by FilterContainer when the user switches a strictMode filter on/off
	strictModeEnabled?: boolean;
	// set at runtime by FilterContainer to restore the filter when strictMode is switched off
	_originalKey?: string;
	_originalCompareMethod?: string;
	_originalTypeData?: string;
	_originalDisableLocalFilter?: boolean;
}

// state of a filter saved in the sessionStorage
export interface FilterState {
	value: FilterValue;
	preset: FilterValue;
	strictModeEnabled: boolean;
}
