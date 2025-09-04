<template>
	<div class="flex flex-wrap sm:justify-start justify-center gap-4 mb-4 max-h-40 overflow-y-auto">
		<Filter v-for="(filter, index) in filters"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:class="filter?.class"
			:options="filter?.options && filter.options.length > 0 ? filterOption(filter) : []"
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
			// - dataType: string (type of data, e.g., 'int', 'float', 'string', 'bool')
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
		Filter : defineAsyncComponent(() => import("@/components/Filter.vue")),
	},
	computed: {
		filteredData() {
			return Object.values(this.storeData).filter((element) => {
				return this.filters.every((f) => {
					if (f.value !== "" && f.value !== null && f.value !== undefined) {
						if (f.subPath) {
							switch (f.compareMethod) {
							case "=":
								return element[f.subPath].some((subElement) => subElement[f.key] === f.value);
							case ">=":
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
							case "<=":
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
							}
						}
						switch (f.compareMethod) {
						case "=":
							switch (f.dataType) {
							case "bool":
								return element[f.key] === (f.value === "true");
							case "int":
								return parseInt(element[f.key]) === parseInt(f.value);
							case "float":
								return parseFloat(element[f.key]) === parseFloat(f.value);
							case "string":
								return element[f.key].toLowerCase() === f.value.toLowerCase();
							}
							return element[f.key] === f.value;
						case ">=":
							return element[f.key] >= f.value;
						case "<=":
							return element[f.key] <= f.value;
						case "contain":
							return element[f.key].includes(f.value);
						}
					}
					return true;
				});
			});
		},
	},
	emits: ["outputFilter"],
	methods: {
		updateText(key, value) {
			this.filters.forEach((filter, index) => {
				if (index === key) {
					switch (filter.type) {
					case "number":
						value = parseFloat(value);
						if (isNaN(value)) {
							value = "";
						}
						break;
					case "text":
						value = value.toLowerCase();
						break;
					case "select":
						switch (filter.typeData) {
						case "int":
							value = parseInt(value);
							if (isNaN(value)) {
								value = "";
							}
							break;
						case "float":
							value = parseFloat(value);
							break;
						case "string":
							value = value.toLowerCase();
							break;
						}
					}
					filter.value = value;
				}
			});
		},
		filterOption(filter) {
			if ((filter.type === "select" || filter.type === "datalist") && filter.options.length > 0) {
				const optionsSet = new Set();
				filter.options.forEach((option) => {
					Object.values(this.storeData).forEach((element) => {
						if (filter.subPath) {
							if (element[filter.subPath].some((subElement) => subElement[filter.key] === option[0])) {
								optionsSet.add(option);
							}
						} else if (element[filter.key] === option[0]) {
							optionsSet.add(option);
						}
					});
				});
				return Array.from(optionsSet);
			}
			return filter.options;
		},
	},
	watch: {
		filteredData: {
			handler(newValue) {
				this.$emit("outputFilter", newValue);
			},
			deep: true,
		},
	},
};
</script>