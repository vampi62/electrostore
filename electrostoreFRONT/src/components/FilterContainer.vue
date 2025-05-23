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
					if (f.value) {
						if (f.subPath) {
							if (f.compareMethod === "=") {
								return element[f.subPath].some((subElement) => subElement[f.key] === f.value);
							} else if (f.compareMethod === ">=") {
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
							} else if (f.compareMethod === "<=") {
								return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
							}
						} else if (f.compareMethod === "=") {
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
					} else if (filter.type === "text") {
						value = value.toLowerCase();
					} else if (filter.type === "select") {
						value = parseInt(value);
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