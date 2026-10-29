import type { RSQLFilter, RSQLSort, RSQLValue } from "@/types/rsql";
import { filledPartialValues, filledValues, filterKeys } from "@/helpers/filterValue";

// a value that contains a delimiter of RSQL is quoted, the quote and the backslash are escaped
function quote(value: RSQLValue): string {
	const text = String(value);
	if (!/[;,()'"]|^\s|\s$/.test(text)) {
		return text;
	}
	return `'${text.replaceAll("\\", "\\\\").replaceAll("'", "\\'")}'`;
}

// RSQL conditions of a filter: none if it has no value, one per value if every value must match.
// "=in=" is a single condition (equal to at least one of the values) in parentheses with the "=like=" of the partial values,
// which are separated by "," (or): (key=in=(a,b),key=like=c,key=like=d)
// with orKeys, each condition is repeated on every key in an or: (key==a,other==a)
function buildRSQLConditions(filter: RSQLFilter): string[] {
	const values = filledValues(filter);
	const keys = filterKeys(filter);
	if (filter.compareMethod === "=in=") {
		const partialValues = filledPartialValues(filter);
		const conditions = keys.flatMap((key) => {
			const likes = partialValues.map((text) => `${key}=like=${quote(text)}`);
			return values.length > 0 ? [`${key}=in=(${values.map(quote).join(",")})`, ...likes] : likes;
		});
		return conditions.length > 1 ? [`(${conditions.join(",")})`] : conditions;
	}
	return values.map((value) => {
		const conditions = keys.map((key) => key + filter.compareMethod + quote(value));
		return conditions.length > 1 ? `(${conditions.join(",")})` : conditions[0];
	});
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
