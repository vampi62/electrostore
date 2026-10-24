import type { RSQLFilter, RSQLSort } from "@/types/rsql";
import { filledValues } from "@/helpers/filterValue";

// RSQL conditions of a filter: none if it has no value, one per value if every value must match,
// one "=any=" condition (equal to at least one of the comma separated values) if at least one value must match
function buildRSQLConditions(filter: RSQLFilter): string[] {
	const values = filledValues(filter);
	if (values.length > 1 && filter.matchMode === "any") {
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
