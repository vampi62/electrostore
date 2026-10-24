export type RSQLValue = string | number | boolean | null | undefined;

export interface RSQLFilter {
	key: string;
	compareMethod: string;
	// an array is a filter with several values, matchMode tells how they are combined
	value: RSQLValue | RSQLValue[];
	// "all": every value must match (default), "any": at least one value must match
	matchMode?: "all" | "any";
}

export interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}
