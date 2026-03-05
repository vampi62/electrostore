
// This function takes an array of filters and builds an RSQL filter string from it
export function buildRSQLFilter(filters) {
	let rsqlFilter = "";
	for (const filter of filters) {
		if (filter.value !== "") {
			const keyApi = filter?.replaceKeyApi ? filter.replaceKeyApi : filter.key;
			if (rsqlFilter === "") {
				rsqlFilter = keyApi + filter.compareMethod + String(filter.value);
			} else {
				rsqlFilter += ";" + keyApi + filter.compareMethod + String(filter.value);
			}
		}
	}
	return rsqlFilter;
}

export function buildRSQLSort(sort) {
	return sort.key + (sort.order === "desc" ? ",desc" : ",asc");
}