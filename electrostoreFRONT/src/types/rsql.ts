export type RSQLValue = string | number | boolean | null | undefined;

export interface RSQLFilter {
	key: string;
	// other fields of the api checked with the same value, the filter is satisfied if the condition holds on the key or on one of these
	orKeys?: string[];
	compareMethod: string;
	// an array is a filter with several values: every value must match, except with "=in=" where at least one must
	value: RSQLValue | RSQLValue[];
	// "=in=" only: texts that a value contains, they are sent with "=like=" and one of them is enough, as one value of the filter
	partialValues?: string[];
	// a value equal to this one is not applied, as if the filter had no value (e.g. false to get everything instead of the rows with a deleted_at)
	disableValue?: RSQLValue;
}

export interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}
