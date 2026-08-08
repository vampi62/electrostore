export function buildQuery({ offset, limit, expand = [], filter, sort, idResearch } = {}) {
	const params = new URLSearchParams();
	if (offset !== undefined) {
		params.set("offset", offset);
	}
	if (limit !== undefined) {
		params.set("limit", limit);
	}
	expand.forEach((e) => params.append("expand", e));
	if (filter) {
		params.set("filter", filter);
	}
	if (sort) {
		params.set("sort", sort);
	}
	if (idResearch && idResearch.length > 0) {
		idResearch.forEach((id) => params.append("idResearch", id));
	}
	return params.toString();
}