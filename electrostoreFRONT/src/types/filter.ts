import type { StateTree } from "pinia";
import type { FetchFunction } from "@/types/fetch";
import type { RSQLFilter, RSQLValue } from "@/types/rsql";

// value of a single input of a filter
export type FilterValue = RSQLValue;

// option of a datalist filter
export interface FilterOption {
	id: string | number;
	value: string;
}

// a filter is sent to the api as it is, so it extends RSQLFilter (key, compareMethod, value)
export interface FilterLabel extends RSQLFilter {
	// translation key for the label
	label: string;
	// input type, e.g. 'text', 'number', 'date', 'datalist', 'checkbox'
	type: string;
	// how the text of the input is converted to the value of the filter: 'number', 'float', 'string', 'bool'
	typeData?: string;
	placeholder?: string;
	class?: string;
	// options of a datalist: { [id]: label }
	options?: Record<string | number, string>;
	sortOptions?: "asc" | "desc";
	fetchOptions?: FetchFunction;
	// store whose data are the options of a datalist, storeKey is the field of an element used as the value and displayed
	storeData?: StateTree;
	storeKey?: string;
	// datalist only: several options can be selected, value is then an array.
	// The options are matched exactly (the compareMethod of a multiple filter should be "==")
	multiple?: boolean;
	// JavaScript expressions evaluated to show / enable the filter
	showCondition?: string;
	enableCondition?: string;
	// the api filters the data, but the table does not filter them again
	disableLocalFilter?: boolean;
	// used by type: "checkbox" to translate the checked state into the filtered value
	valueIfTrue?: FilterValue;
	valueIfFalse?: FilterValue;
}

// state of a filter saved in the sessionStorage
export interface FilterState {
	value: FilterLabel["value"];
}
