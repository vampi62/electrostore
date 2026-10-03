interface RSQLFilter {
	key: string;
	compareMethod: string;
	value: any;
}

interface RSQLSort {
	key?: string;
	order?: "asc" | "desc";
}

export type { RSQLFilter, RSQLSort };