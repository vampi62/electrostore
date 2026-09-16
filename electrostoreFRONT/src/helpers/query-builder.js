export function buildQuery({ offset, limit, expand = [], filter, sort, idResearch = [] } = {}) {
	const params = new URLSearchParams();
	if (offset !== undefined) {
		params.set("offset", offset);
	}
	if (limit !== undefined) {
		params.set("limit", limit);
	}
	for (const e of expand) {
		params.append("expand", e);
	}
	if (filter) {
		params.set("filter", filter);
	}
	if (sort) {
		params.set("sort", sort);
	}
	for (const id of idResearch) {
		params.append("idResearch", id);
	}
	return params.toString();
}