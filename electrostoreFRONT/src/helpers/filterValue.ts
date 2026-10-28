import { toLowerCaseWithoutAccents } from "@/utils";
import type { FilterLabel, FilterValue } from "@/types/filter";
import type { RSQLFilter, RSQLValue } from "@/types/rsql";

// a filter without value is not applied
export function isFilled(value: RSQLValue): boolean {
	return value !== "" && value !== null && value !== undefined;
}

// the inputs only give strings, so "true" disables a filter whose disableValue is true
function isDisabledValue(filter: Pick<RSQLFilter, "disableValue">, value: RSQLValue): boolean {
	return filter.disableValue !== undefined && String(value) === String(filter.disableValue);
}

// values of a filter that are applied, a filter that is not multiple has at most one
export function filledValues(filter: Pick<RSQLFilter, "value" | "disableValue">): RSQLValue[] {
	const values = Array.isArray(filter.value) ? filter.value : [filter.value];
	return values.filter((value) => isFilled(value) && !isDisabledValue(filter, value));
}

// keys of the api that a filter checks, the first one is its key
export function filterKeys(filter: Pick<RSQLFilter, "key" | "orKeys">): string[] {
	return [filter.key, ...(filter.orKeys ?? [])];
}

// texts of a filter that are applied, a text that only has spaces is not
export function filledPartialValues(filter: Pick<RSQLFilter, "partialValues">): string[] {
	return (filter.partialValues ?? []).filter((text) => text.trim() !== "");
}

function toNumber(text: string, parse: (text: string) => number): number | "" {
	const number = parse(text);
	return Number.isNaN(number) ? "" : number;
}

// convert the text of a list or hidden filter according to the typeData of the filter, without typeData it is left as it is
function toTypedValue(typeData: string | undefined, raw: FilterValue, text: string): FilterValue {
	switch (typeData) {
	case "number":
		return toNumber(text, Number.parseInt);
	case "float":
		return toNumber(text, Number.parseFloat);
	case "string":
		return text.toLowerCase();
	case "bool":
		return text === "" ? "" : text === "true";
	default:
		return raw;
	}
}

// convert what the user typed or chose to the value of the filter, according to the type of the filter
export function toFilterValue(filter: Pick<FilterLabel, "type" | "typeData" | "valueIfTrue" | "valueIfFalse">, raw: FilterValue): FilterValue {
	const text = String(raw ?? "");
	switch (filter.type) {
	case "number":
		return toNumber(text, Number.parseFloat);
	case "text":
		return text.toLowerCase();
	case "checkbox":
		return raw ? filter.valueIfTrue : filter.valueIfFalse;
	case "select":
	case "datalist":
	case "hidden":
		return toTypedValue(filter.typeData, raw, text);
	default:
		return raw;
	}
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
	case "=in=":
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

// does a row satisfy a filter, with several values the row must match all of them,
// or at least one of the values and of the partial values for "=in="
// a value of the row can be the one of any key of the filter (key and orKeys)
// the values of the row are only read if the filter is applied
export function matchFilter(filter: RSQLFilter & Pick<FilterLabel, "disableLocalFilter">, getRowValue: (key: string) => unknown): boolean {
	const values = filledValues(filter);
	const isIn = filter.compareMethod === "=in=";
	const partialValues = isIn ? filledPartialValues(filter) : [];
	if ((values.length === 0 && partialValues.length === 0) || filter.disableLocalFilter) {
		return true;
	}
	const rowValues = filterKeys(filter).map((key) => getRowValue(key));
	const matches = (rowValue: unknown, value: RSQLValue) => matchValue(filter.compareMethod, rowValue, value as NonNullable<RSQLValue>);
	if (isIn) {
		return rowValues.some((rowValue) => values.some((value) => matches(rowValue, value))
			|| partialValues.some((text) => matchValue("=like=", rowValue, text)));
	}
	return values.every((value) => rowValues.some((rowValue) => matches(rowValue, value)));
}
