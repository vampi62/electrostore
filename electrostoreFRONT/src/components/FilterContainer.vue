<template>
	<div class="flex flex-wrap">
		<Filter v-for="(filter, index) in filters"
			:key="index"
			:label="filter.label"
			:type="filter.type"
			:placeholder="filter?.placeholder"
			:class-css="filter?.class"
			:class="filter?.class"
			:options="filter?.options"
			:idKey="index"
			@update-text="updateText"
		/>
	</div>
</template>
<script>
import Filter from "./Filter.vue";
export default {
	name: "FilterContainer",
	props: {
		filters: {
			type: Array,
			required: true,
		},
		storeData: {
			type: Object,
			required: true,
		},
	},
	components: {
		Filter,
	},
	computed: {
		filteredData() {
			return Object.values(this.storeData).filter((element) => {
				return this.filters.every((f) => {
					if (f.value !== "" && f.value !== null && f.value !== undefined) {
						if (f.subPath) {
							if (f.compareMethod === "=") {
								return element[f.subPath].some((subElement) => subElement[f.key] === f.value);
							} else if (f.compareMethod === ">=") {
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
							} else if (f.compareMethod === "<=") {
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
							}
						} else if (f.compareMethod === "=") {
							if (f.dataType === "bool") {
								return element[f.key] === (f.value === "true");
							} else if (f.dataType === "int") {
								return parseInt(element[f.key]) === parseInt(f.value);
							} else if (f.dataType === "float") {
								return parseFloat(element[f.key]) === parseFloat(f.value);
							} else if (f.dataType === "string") {
								return element[f.key].toLowerCase() === f.value.toLowerCase();
							}
							return element[f.key] === f.value;
						} else if (f.compareMethod === ">=") {
							return element[f.key] >= f.value;
						} else if (f.compareMethod === "<=") {
							return element[f.key] <= f.value;
						} else if (f.compareMethod === "contain") {
							return element[f.key].includes(f.value);
						}
					}
					return true;
				});
			});
		},
	},
	emits: ["output-filter"],
	methods: {
		updateText(key, value) {
			this.filters.forEach((filter, index) => {
				if (index === key) {
					if (filter.type === "number") {
						value = parseFloat(value);
						if (isNaN(value)) {
							value = "";
						}
					} else if (filter.type === "text") {
						value = value.toLowerCase();
					} else if (filter.type === "select") {
						if (filter.typeData === "int") {
							value = parseInt(value);
							if (isNaN(value)) {
								value = "";
							}
						} else if (filter.typeData === "float") {
							value = parseFloat(value);
						} else if (filter.typeData === "string") {
							value = value.toLowerCase();
						}
					}
					filter.value = value;
				}
			});
		},
	},
	watch: {
		filteredData: {
			handler(newValue) {
				this.$emit("output-filter", newValue);
			},
			deep: true,
		},
	},
};
</script>