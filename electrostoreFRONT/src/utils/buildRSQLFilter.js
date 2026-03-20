
// This function takes an array of filters and builds an RSQL filter string from it
export function buildRSQLFilter(filters) {
	let rsqlFilter = "";
	for (const filter of filters) {
		if (filter.value !== "") {
			if (rsqlFilter === "") {
				rsqlFilter = filter.key + filter.compareMethod + String(filter.value);
			} else {
				rsqlFilter += ";" + filter.key + filter.compareMethod + String(filter.value);
			}
		}
	}
	return rsqlFilter;
}

export function buildRSQLSort(sort) {
	if (sort?.key === undefined || sort?.key === null || sort?.key === "") {
		return "";
	}
	return sort.key + (sort.order === "desc" ? ",desc" : ",asc");
}