<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 max-h-20 overflow-y-auto pb-2">
		<Filter v-for="(filter, index) in filtersShown"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:disabled="filter?.enableCondition !== undefined && !eval(filter.enableCondition)"
			:preset="filter?.preset"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:options="filter?.options ? Object.keys(filter.options).map((key) => ({ id: key, value: filter.options[key] })) : null"
			:sort-options="filter?.sortOptions"
			:fetch-options="filter?.fetchOptions"
			:store-data="filter?.storeData"
			:store-key="filter?.storeKey"
			:strict-mode="filter.strictMode"
			@update-text="(value, mode) => updateText(index, value, mode)"
		/>
	</div>
</template>

<script>
import { defineAsyncComponent } from "vue";
export default {
	name: "FilterContainer",
	props: {
		filters: {
			type: Array,
			required: true,
			// This should be an array of filter objects, each containing:
			// - key: string (the key in the storeData to filter on)
			// - label: string (translation key for the label)
			// - type: string (input type, e.g., 'text', 'number', 'select')
			// - typeData: string (type of data, e.g., 'number', 'float', 'string', 'bool') required if type is 'select'
			// - compareMethod: string (comparison method, e.g., '==', '=ge=', '=le=', '=like=')
			// - value: any (the value to filter by, can be empty)
			// - placeholder: string (translation key for the placeholder, optional)
			// - class: string (tailwind CSS class for styling, optional)
			// - options: array (for select inputs, optional, required if type is 'select')
			// - sortOptions: boolean (optional, if true, options will be sorted alphabetically by their value)
			// - fetchOptions: function (optional, required if type is 'select' or 'datalist' and options is not provided) that returns a promise resolving to an array of options
			// - storeData: pinia store (optional, required if fetchOptions is provided) the store whose data will be received by the fetchOptions function
			// - storeKey: string (optional, required if fetchOptions is provided) the key in storeData to pass to fetchOptions function
			default: () => [],
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
				filter._originalKey = filter.key;
				filter._originalCompareMethod = filter.compareMethod;
				if (filter.typeData) {
					filter._originalTypeData = filter.typeData;
				}
				if (filter.disableLocalFilter) {
					filter._disableLocalFilter = filter.disableLocalFilter;
				}
			}
		}
		if (this.saveState) {
			const saved = sessionStorage.getItem(this._filterStateKey());
			if (saved) {
				const savedValues = JSON.parse(saved);
				for (const filter of this.filters) {
					if (filter.key in savedValues) {
						//prevent malformed saved values from breaking the component
						if (typeof savedValues[filter.key].value === "undefined" || typeof savedValues[filter.key].value === "object") {
							continue;
						}
						if (savedValues[filter.key].value === null || savedValues[filter.key].value === "undefined") {
							savedValues[filter.key].value = "";
						}
						filter.value = savedValues[filter.key].value;
						filter.preset = savedValues[filter.key].preset;
						filter.strictModeEnabled = savedValues[filter.key].strictModeEnabled || false;
						if (filter.strictMode && filter.strictModeEnabled) {
							filter.key = filter.strictMode.key;
							filter.compareMethod = "==";
							filter.typeData = filter.strictMode.typeData;
							filter.disableLocalFilter = filter.strictMode.disableLocalFilter || false;
						}
					}
				}
			}
		}
		this.$emit("ready");
	},
	methods: {
		_filterStateKey() {
			return `filter_state_${this.$route?.path || ""}_${this.stateKey || "default"}`;
		},
		_persistFilters() {
			const values = {};
			for (const filter of this.filters) {
				if (filter.key !== undefined) {
					if (filter.strictMode) {
						values[filter._originalKey] = { value: filter.value, preset: filter.preset, strictModeEnabled: filter.strictModeEnabled || false };
					} else {
						values[filter.key] = { value: filter.value, preset: filter.preset, strictModeEnabled: false };
					}
				}
			}
			sessionStorage.setItem(this._filterStateKey(), JSON.stringify(values));
		},
		_validateValue(filter, value) {
			switch (filter.type) {
			case "number":
				value = Number.parseFloat(value);
				if (Number.isNaN(value)) {
					value = "";
				}
				break;
			case "text":
				value = value.toLowerCase();
				break;
			case "select":
			case "datalist":
			case "hidden":
				switch (filter?.typeData) {
				case "number":
					value = Number.parseInt(value);
					if (Number.isNaN(value)) {
						value = "";
					}
					break;
				case "float":
					value = Number.parseFloat(value);
					break;
				case "string":
					value = value.toLowerCase();
					break;
				case "bool":
					value = value === "true";
					break;
				}
				break;
			case "checkbox":
				value = value ? filter.valueIfTrue : filter.valueIfFalse;
				break;
			}
			filter.value = value;
		},
		updateText(key, value, mode = "text") {
			for (const [index, filter] of this.filters.entries()) {
				if (index === key) {
					if (mode === "select") {
						if (filter.strictMode) {
							filter.strictModeEnabled = true;
							filter.key = filter.strictMode.key;
							filter.compareMethod = "==";
							filter.typeData = filter.strictMode.typeData;
							filter.disableLocalFilter = filter.strictMode.disableLocalFilter || false;
						}
					}
					if (mode === "text" || (mode === "select" && !filter?.strictModeEnabled)) {
						if (filter.strictModeEnabled) {
							filter.strictModeEnabled = false;
							filter.key = filter._originalKey;
							filter.compareMethod = filter._originalCompareMethod;
							if (filter._originalTypeData) {
								filter.typeData = filter._originalTypeData;
							} else {
								delete filter.typeData;
							}
							if (filter._disableLocalFilter) {
								filter.disableLocalFilter = filter._disableLocalFilter;
							} else {
								delete filter.disableLocalFilter;
							}
						}
					}
					this._validateValue(filter, value[0]);
					filter.preset = value[1] || null;
					break;
				}
			}
			if (this.saveState) {
				this._persistFilters();
			}
		},
	},
};
</script>