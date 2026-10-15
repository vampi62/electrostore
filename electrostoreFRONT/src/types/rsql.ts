export interface RSQLFilter {
	key: string;
	compareMethod: string;
	value: string | number | boolean | null | undefined;
}

export interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}
