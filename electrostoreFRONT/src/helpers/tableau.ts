import type { StateTree } from "pinia";
import type { TableauRowData } from "@/types/tableau";

// Row as it should be displayed: the saved row overlaid with the non-empty fields of the unsaved change (storeReady).
export function mergeReadyRow(row: TableauRowData, storeReady?: StateTree): TableauRowData {
	if (!storeReady?.status || !storeReady?.data) {
		return row;
	}
	return {
		...row,
		...Object.fromEntries(
			Object.entries(storeReady.data).filter(([, v]) => v !== undefined && v !== null),
		),
	};
}
