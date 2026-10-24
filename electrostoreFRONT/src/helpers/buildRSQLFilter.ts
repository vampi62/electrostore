import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import { filledValues } from "@/helpers/filterValue";

// RSQL conditions of a filter: none if it has no value, one per value if every value must match,
// a single condition with the comma separated values for "=any=" (equal to at least one of the values)
function buildRSQLConditions(filter: RSQLFilter): string[] {
	const values = filledValues(filter);
	if (filter.compareMethod === "=any=" && values.length > 0) {
		return [`${filter.key}=any=${values.join(",")}`];
	}
	return values.map((value) => filter.key + filter.compareMethod + String(value));
}

// This function takes an array of filters and builds an RSQL filter string from it
export function buildRSQLFilter(filters: RSQLFilter[]) {
	return filters.flatMap(buildRSQLConditions).join(";");
}

export function buildRSQLSort(sort?: RSQLSort) {
	if (sort?.key === undefined || sort?.key === null || sort?.key === "") {
		return "";
	}
	return sort.key + (sort.order === "desc" ? ",desc" : ",asc");
}
