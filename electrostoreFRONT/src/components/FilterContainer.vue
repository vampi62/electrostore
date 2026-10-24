<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 max-h-20 overflow-y-auto pb-2">
		<Filter v-for="filter in filtersShown"
			:key="filters.indexOf(filter)"
			:label="filter.label"
			:type="filter.type"
			:disabled="filter.enableCondition !== undefined && !evalCondition(filter.enableCondition)"
			:model-value="inputValue(filter)"
			:placeholder="filter.placeholder"
			:class-css="filter.class"
			:options="optionsOf(filter)"
			:sort-options="filter.sortOptions"
			:fetch-options="filter.fetchOptions"
			:store-data="filter.storeData"
			:store-key="filter.storeKey"
			:multiple="filter.multiple"
			@update="(value) => updateFilter(filter, value)"
		/>
	</div>
</template>

<script lang="ts">
import { defineAsyncComponent } from "vue";
import type { PropType } from "vue";
import { isFilled, toFilterValue } from "@/helpers";
import type { FilterLabel, FilterOption, FilterState, FilterValue } from "@/types/filter";
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
				return this.evalCondition(filter.showCondition);
			});
		},
	},
	emits: ["ready"],
	components: {
		Filter: defineAsyncComponent(() => import("@/components/Filter.vue")),
	},
	beforeMount() {
		if (this.saveState) {
			this.restoreFilters();
		}
		this.$emit("ready");
	},
	methods: {
		// bare eval() in a template resolves to the (nonexistent) this.eval, not the global eval — must go through a method
		evalCondition(condition: string) {
			return eval(condition);
		},
		optionsOf(filter: FilterLabel): FilterOption[] | undefined {
			if (!filter.options) {
				return undefined;
			}
			const options = filter.options;
			return Object.keys(options).map((key) => ({ id: key, value: options[key] }));
		},
		// the checkbox displays if the value is the one of a checked box, the other inputs display the value
		inputValue(filter: FilterLabel): FilterLabel["value"] {
			if (filter.type === "checkbox") {
				return filter.valueIfTrue !== undefined && filter.value === filter.valueIfTrue;
			}
			return filter.value;
		},
		updateFilter(filter: FilterLabel, raw: FilterValue | FilterValue[]) {
			filter.value = Array.isArray(raw)
				? raw.map((value) => toFilterValue(filter, value)).filter(isFilled)
				: toFilterValue(filter, raw);
			if (this.saveState) {
				this.persistFilters();
			}
		},
		_filterStateKey() {
			return `filter_state_${this.$route?.path || ""}_${this.stateKey || "default"}`;
		},
		// several filters can have the same key (min / max), so the filters are identified by their position
		_filterId(filter: FilterLabel) {
			return `${this.filters.indexOf(filter)}_${filter.key}`;
		},
		persistFilters() {
			const state: Record<string, FilterState> = {};
			for (const filter of this.filters) {
				state[this._filterId(filter)] = { value: filter.value };
			}
			sessionStorage.setItem(this._filterStateKey(), JSON.stringify(state));
		},
		restoreFilters() {
			let state: Record<string, FilterState>;
			try {
				state = JSON.parse(sessionStorage.getItem(this._filterStateKey()) ?? "{}");
			} catch {
				return;
			}
			for (const filter of this.filters) {
				const value = state[this._filterId(filter)]?.value;
				// prevent malformed saved values from breaking the component
				if (Array.isArray(value) ? filter.multiple : (value !== undefined && typeof value !== "object")) {
					filter.value = value ?? "";
				}
			}
		},
	},
};
</script>
