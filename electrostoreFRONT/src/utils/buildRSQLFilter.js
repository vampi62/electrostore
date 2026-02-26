
// This function takes an array of filters and builds an RSQL filter string from it
export function buildRSQLFilter(filters) {
	let rsqlFilter = "";
	for (const filter of filters) {
		if (filter.value !== "") {
			let newFilter = "";
			newFilter = filter.key;
			if (filter.compareMethod === "contain") {
				newFilter += "=like=" + filter.value;
			} else if (filter.compareMethod === "=") {
				newFilter += "==" + filter.value;
			} else if (filter.compareMethod === ">=") {
				newFilter += "=ge=" + filter.value;
			} else if (filter.compareMethod === "<=") {
				newFilter += "=le=" + filter.value;
			}
			if (rsqlFilter === "") {
				rsqlFilter = newFilter;
			} else {
				rsqlFilter += ";" + newFilter;
			}
		}
	}
	return rsqlFilter;
}

export function buildRSQLSort(sort) {
	if (sort.column) {
		return sort.column.key + (sort.order === "desc" ? ",desc" : ",asc");
	}
	return "";
}