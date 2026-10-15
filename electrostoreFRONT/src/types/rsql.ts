interface RSQLFilter {
	key: string;
	compareMethod: string;
	value: string | number | boolean | null | undefined;
}

interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}

export type { RSQLFilter, RSQLSort };