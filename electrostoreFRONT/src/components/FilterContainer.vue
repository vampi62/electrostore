<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 mb-4 max-h-40 overflow-y-auto">
		<Filter v-for="(filter, index) in filters"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:placeholder="filter?.placeholder"
			:preset="filter.value"
			:class-css="filter?.class"
			:class="filter?.class"
			:options="filter?.options && Object.keys(filter.options).length > 0 ? filterOption(filter) : null"
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
			// - typeData: string (type of data, e.g., 'int', 'float', 'string', 'bool')
			// - compareMethod: string (comparison method, e.g., '=', '>=', '<=', 'contain')
			// - value: any (the value to filter by, can be empty)
			// - subPath: string (optional, for nested data filtering)
			// - placeholder: string (translation key for the placeholder, optional)
			// - class: string (tailwind CSS class for styling, optional)
			// - options: array (for select inputs, optional, required if type is 'select')
			default: () => [],
		},
		storeData: {
			type: Object,
			required: true,
			// This should be an object containing the data to filter
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
						}
					}
					filter.value = value;
				}
			}
		},
		filterOption(filter) {
			if (filter?.onlyUsed && (filter.type === "select" || filter.type === "datalist") && Object.keys(filter.options).length > 0) {
				const optionsSet = new Set();
				for (const [index, option] of Object.entries(filter.options)) {
					Object.values(this.storeData).forEach((element) => {
						if (filter.subPath) {
							if (Array.isArray(element[filter.subPath]) && (element[filter.subPath].some((subElement) => subElement[filter.key] === index))) {
								optionsSet.add(option);
							}
						} else if (element[filter.key] === index) {
							optionsSet.add(option);
						}
					});
				}
				return Array.from(optionsSet);
			}
			return filter.options;
		},
	},
};
</script>