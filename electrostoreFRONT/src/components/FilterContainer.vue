<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 max-h-20 overflow-y-auto pb-2">
		<Filter v-for="(filter, index) in filtersShown"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:disabled="filter?.enableCondition !== undefined && !evalCondition(filter.enableCondition)"
			:preset="filter?.preset"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:options="filter?.options ? Object.keys(filter.options).map((key) => ({ id: key, value: filter.options![key] })) : undefined"
			:sort-options="filter?.sortOptions"
			:fetch-options="filter?.fetchOptions"
			:store-data="filter?.storeData"
			:store-key="filter?.storeKey"
			:strict-mode="filter.strictMode"
			@update-text="(value, mode) => updateText(filter, value, mode)"
		/>
	</div>
</template>

<script lang="ts">
import { defineAsyncComponent } from "vue";
import type { PropType } from "vue";
import type { FilterLabel, FilterState, FilterValue } from "@/types/filter";
export default {
	name: "FilterContainer",
	props: {
		filters: {
			type: Array as PropType<FilterLabel[]>,
			required: true,
		},
		saveState: {
			type: Boolean,
			default: false,
		},
		stateKey: {
			type: String,
			default: null,
			// optional unique key to identify this filter set in sessionStorage
		},
	},
	computed: {
		filtersShown() {
			return this.filters.filter((filter) => {
				if (filter.showCondition === undefined) {
					return true;
				}
				return eval(filter.showCondition);
			});
		},
	},
	emits: ["ready"],
	components: {
		Filter: defineAsyncComponent(() => import("@/components/Filter.vue")),
	},
	beforeMount() {
		for (const filter of this.filters) {
			if (filter.strictMode) {
				this._saveOriginalConfig(filter);
			}
		}
		if (this.saveState) {
			this._restoreSavedState();
		}
		this.$emit("ready");
	},
	methods: {
		_saveOriginalConfig(filter: FilterLabel) {
			filter._originalKey = filter.key;
			filter._originalCompareMethod = filter.compareMethod;
			if (filter.typeData) {
				filter._originalTypeData = filter.typeData;
			}
			if (filter.disableLocalFilter) {
				filter._originalDisableLocalFilter = filter.disableLocalFilter;
			}
		},
		_restoreSavedState() {
			const saved = sessionStorage.getItem(this._filterStateKey());
			if (!saved) {
				return;
			}
			const savedValues: Record<string, FilterState> = JSON.parse(saved);
			for (const filter of this.filters) {
				const state = savedValues[filter.key];
				//prevent malformed saved values from breaking the component
				if (!(filter.key in savedValues) || state.value === undefined || typeof state.value === "object") {
					continue;
				}
				filter.value = state.value === null || state.value === "undefined" ? "" : state.value;
				filter.preset = state.preset;
				filter.strictModeEnabled = state.strictModeEnabled || false;
				if (filter.strictModeEnabled) {
					this._enableStrictMode(filter);
				}
			}
		},
		_enableStrictMode(filter: FilterLabel) {
			if (!filter.strictMode) {
				return;
			}
			filter.strictModeEnabled = true;
			filter.key = filter.strictMode.key;
			filter.compareMethod = "==";
			filter.typeData = filter.strictMode.typeData;
			filter.disableLocalFilter = filter.strictMode.disableLocalFilter || false;
		},
		_disableStrictMode(filter: FilterLabel) {
			filter.strictModeEnabled = false;
			filter.key = filter._originalKey ?? filter.key;
			filter.compareMethod = filter._originalCompareMethod ?? filter.compareMethod;
			if (filter._originalTypeData) {
				filter.typeData = filter._originalTypeData;
			} else {
				delete filter.typeData;
			}
			if (filter._originalDisableLocalFilter) {
				filter.disableLocalFilter = filter._originalDisableLocalFilter;
			} else {
				delete filter.disableLocalFilter;
			}
		},
		// bare eval() in a template resolves to the (nonexistent) this.eval, not the global eval — must go through a method
		evalCondition(condition: string) {
			return eval(condition);
		},
		_filterStateKey() {
			return `filter_state_${this.$route?.path || ""}_${this.stateKey || "default"}`;
		},
		_persistFilters() {
			const values: Record<string, FilterState> = {};
			for (const filter of this.filters) {
				if (filter.key !== undefined) {
					if (filter.strictMode) {
						values[filter._originalKey ?? filter.key] = { value: filter.value, preset: filter.preset, strictModeEnabled: filter.strictModeEnabled || false };
					} else {
						values[filter.key] = { value: filter.value, preset: filter.preset, strictModeEnabled: false };
					}
				}
			}
			sessionStorage.setItem(this._filterStateKey(), JSON.stringify(values));
		},
		_validateValue(filter: FilterLabel, rawValue: FilterValue) {
			// the inputs give a string, the checkbox a boolean
			const text = String(rawValue ?? "");
			let value: FilterValue = rawValue;
			switch (filter.type) {
			case "number":
				value = Number.parseFloat(text);
				if (Number.isNaN(value)) {
					value = "";
				}
				break;
			case "text":
				value = text.toLowerCase();
				break;
			case "select":
			case "datalist":
			case "hidden":
				switch (filter?.typeData) {
				case "number":
					value = Number.parseInt(text);
					if (Number.isNaN(value)) {
						value = "";
					}
					break;
				case "float":
					value = Number.parseFloat(text);
					break;
				case "string":
					value = text.toLowerCase();
					break;
				case "bool":
					value = text === "true";
					break;
				}
				break;
			case "checkbox":
				value = rawValue ? filter.valueIfTrue : filter.valueIfFalse;
				break;
			}
			filter.value = value;
		},
		updateText(filter: FilterLabel, value: [FilterValue, FilterValue], mode: "text" | "select" = "text") {
			if (mode === "select") {
				this._enableStrictMode(filter);
			} else if (filter.strictModeEnabled) {
				this._disableStrictMode(filter);
			}
			this._validateValue(filter, value[0]);
			filter.preset = value[1] || null;
			if (this.saveState) {
				this._persistFilters();
			}
		},
	},
};
</script>
