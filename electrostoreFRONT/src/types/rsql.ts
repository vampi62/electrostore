export type RSQLValue = string | number | boolean | null | undefined;

export interface RSQLFilter {
	key: string;
	compareMethod: string;
	// an array is a filter with several values: every value must match, except with "=in=" where at least one must
	value: RSQLValue | RSQLValue[];
	// "=in=" only: texts that a value contains, they are sent with "=like=" and one of them is enough, as one value of the filter
	partialValues?: string[];
}

export interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}
