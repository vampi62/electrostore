import type { RSQLFilter, RSQLSort } from "@/types/rsql";

export interface BuildQueryOptions {
	offset?: number;
	limit?: number;
	expand?: string[];
	filter?: RSQLFilter[];
	sort?: RSQLSort;
	idResearch?: (string | number)[];
}
