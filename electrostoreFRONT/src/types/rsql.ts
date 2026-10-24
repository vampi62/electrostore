export type RSQLValue = string | number | boolean | null | undefined;

export interface RSQLFilter {
	key: string;
	compareMethod: string;
	// an array is a filter with several values: every value must match, except with "=any=" where at least one must
	value: RSQLValue | RSQLValue[];
}

export interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}
