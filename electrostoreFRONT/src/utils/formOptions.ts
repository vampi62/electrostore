import { toValue } from "vue";
import type { MaybeRef } from "vue";
import type { FormOptions } from "@/types/form";

// [value, label, disabled, hidden] of an option
export type SortedOption = [string, string, boolean, boolean];

interface OptionsSource {
	options?: MaybeRef<FormOptions>;
	sort?: "asc" | "desc";
}

export function getSortedOptions(source: OptionsSource): SortedOption[] {
	const options = toValue(source.options);
	if (!options) {
		return [];
	}

	const entries = Object.entries(options).map(([key, value]): SortedOption => {
		if (typeof value === "object" && value !== null) {
			return [key, value.label || "", value.disabled || false, value.hidden || false];
		}
		return [key, String(value), false, false];
	});

	if (!source.sort) {
		return entries;
	}

	const direction = source.sort === "asc" ? 1 : -1;
	return entries.sort((a, b) => direction * a[1].localeCompare(b[1]));
}
