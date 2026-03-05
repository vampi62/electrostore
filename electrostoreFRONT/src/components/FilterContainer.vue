<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 mb-4 max-h-40 overflow-y-auto">
		<Filter v-for="(filter, index) in filters"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:preset="filter?.value"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:options="filter?.options"
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
			// - typeData: string (type of data, e.g., 'int', 'float', 'string', 'bool') required if type is 'select'
			// - compareMethod: string (comparison method, e.g., '==', '=ge=', '=le=', '=like=')
			// - value: any (the value to filter by, can be empty)
			// - placeholder: string (translation key for the placeholder, optional)
			// - class: string (tailwind CSS class for styling, optional)
			// - options: array (for select inputs, optional, required if type is 'select')
			//todo// - fetchOptions: function (optional, required if type is 'select' and options is not provided) that returns a promise resolving to an array of options
			//todo// - storeData: pinia store (optional, required if fetchOptions is provided) the store whose data will be received by the fetchOptions function
			//todo// - storeKey: string (optional, required if fetchOptions is provided) the key in storeData to pass to fetchOptions function
			default: () => [],
		},
	},
	components: {
		Filter: defineAsyncComponent(() => import("@/components/Filter.vue")),
	},
	methods: {
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
						case "int":
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
		},
	},
};
</script>