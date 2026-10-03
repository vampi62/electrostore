import type { BuildQueryOptions } from "@/types/query";
import { buildRSQLFilter, buildRSQLSort } from "@/helpers/buildRSQLFilter";

export function buildQuery({ offset, limit, expand = [], filter, sort, idResearch = [] }: BuildQueryOptions = {}) {
	const params = new URLSearchParams();
	if (offset !== undefined) {
		params.set("offset", String(offset));
	}
	if (limit !== undefined) {
		params.set("limit", String(limit));
	}
	for (const e of expand) {
		params.append("expand", e);
	}
	if (filter) {
		params.set("filter", buildRSQLFilter(filter));
	}
	if (sort) {
		params.set("sort", buildRSQLSort(sort));
	}
	for (const id of idResearch) {
		params.append("idResearch", String(id));
	}
	return params.toString();
}
