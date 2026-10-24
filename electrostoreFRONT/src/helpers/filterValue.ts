import { toLowerCaseWithoutAccents } from "@/utils";
import type { FilterLabel, FilterValue } from "@/types/filter";
import type { RSQLFilter, RSQLValue } from "@/types/rsql";

// a filter without value is not applied
export function isFilled(value: RSQLValue): boolean {
	return value !== "" && value !== null && value !== undefined;
}

// values of a filter that are applied, a filter that is not multiple has at most one
export function filledValues(filter: Pick<RSQLFilter, "value">): RSQLValue[] {
	const values = Array.isArray(filter.value) ? filter.value : [filter.value];
	return values.filter(isFilled);
}

// convert what the user typed or chose to the value of the filter, according to the type of the filter
export function toFilterValue(filter: Pick<FilterLabel, "type" | "typeData" | "valueIfTrue" | "valueIfFalse">, raw: FilterValue): FilterValue {
	const text = String(raw ?? "");
	switch (filter.type) {
	case "number": {
		const number = Number.parseFloat(text);
		return Number.isNaN(number) ? "" : number;
	}
	case "text":
		return text.toLowerCase();
	case "checkbox":
		return raw ? filter.valueIfTrue : filter.valueIfFalse;
	case "select":
	case "datalist":
	case "hidden":
		switch (filter.typeData) {
		case "number": {
			const number = Number.parseInt(text);
			return Number.isNaN(number) ? "" : number;
		}
		case "float": {
			const number = Number.parseFloat(text);
			return Number.isNaN(number) ? "" : number;
		}
		case "string":
			return text.toLowerCase();
		case "bool":
			return text === "" ? "" : text === "true";
		}
	}
	return raw;
}

function normalize(value: unknown): string {
	return toLowerCaseWithoutAccents(String(value ?? ""));
}

// does the value of a row satisfy one value of a filter, an array (list of linked resources) satisfies it if one of its elements does
function matchValue(compareMethod: string, rowValue: unknown, value: NonNullable<RSQLValue>): boolean {
	if (Array.isArray(rowValue)) {
		return rowValue.some((element) => matchValue(compareMethod, element, value));
	}
	switch (compareMethod) {
	case "==":
	case "=any=":
		// "1" must match 1 and "true" must match true, the inputs only give strings
		return normalize(rowValue) === normalize(value);
	case "=like=":
		return normalize(rowValue).includes(normalize(value));
	case "=ge=":
		return isFilled(rowValue as RSQLValue) && (rowValue as number | string) >= (value as number | string);
	case "=le=":
		return isFilled(rowValue as RSQLValue) && (rowValue as number | string) <= (value as number | string);
	default:
		// a compare method that can't be evaluated here is left to the api
		return true;
	}
}

// does a row satisfy a filter, with several values the row must match all of them, or at least one for "=any="
// the value of the row is only read if the filter is applied
export function matchFilter(filter: RSQLFilter & Pick<FilterLabel, "disableLocalFilter">, getRowValue: () => unknown): boolean {
	const values = filledValues(filter);
	if (values.length === 0 || filter.disableLocalFilter) {
		return true;
	}
	const rowValue = getRowValue();
	const matches = (value: RSQLValue) => matchValue(filter.compareMethod, rowValue, value as NonNullable<RSQLValue>);
	return filter.compareMethod === "=any=" ? values.some(matches) : values.every(matches);
}
