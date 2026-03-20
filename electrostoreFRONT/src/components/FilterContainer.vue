<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 mb-4 max-h-40 overflow-y-auto">
		<Filter v-for="(filter, index) in filters"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:preset="filter?.value"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:options="filter?.options ? Object.keys(filter.options).map((key) => ({ id: key, value: filter.options[key] })) : null"
			:sort-options="filter?.sortOptions"
			:fetch-options="filter?.fetchOptions"
			:store-data="filter?.storeData"
			:store-key="filter?.storeKey"
			@update-text="(value) => updateText(index, value)"
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
			// - compareMethod: string (comparison method, e.g., '==', '=ge=', '=le=', '=like=')
			// - value: any (the value to filter by, can be empty)
			// - placeholder: string (translation key for the placeholder, optional)
			// - class: string (tailwind CSS class for styling, optional)
			// - options: array (for select inputs, optional, required if type is 'select')
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
	emits: ["ready"],
	components: {
		Filter: defineAsyncComponent(() => import("@/components/Filter.vue")),
	},
	beforeMount() {
		if (this.saveState) {
			const saved = sessionStorage.getItem(this._filterStateKey());
			if (saved) {
				const savedValues = JSON.parse(saved);
				for (const filter of this.filters) {
					if (filter.key in savedValues) {
						filter.value = savedValues[filter.key];
						filter.preset = savedValues[filter.key];
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
					values[filter.key] = filter.value;
				}
			}
			sessionStorage.setItem(this._filterStateKey(), JSON.stringify(values));
		},
		updateText(key, value) {
			for (const [index, filter] of this.filters.entries()) {
				if (index === key) {
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
						switch (filter.typeData) {
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
				}
			}
			if (this.saveState) {
				this._persistFilters();
			}
		},
	},
};
</script>