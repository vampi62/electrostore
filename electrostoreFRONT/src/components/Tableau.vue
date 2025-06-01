<template>
	<table :class="tableauCss.table">
		<thead :class="tableauCss.thead">
			<tr>
				<th v-for="(column,index) in labels"
					:key="index"
					:class="[tableauCss.th, column.sortable ? 'cursor-pointer' : '']"
					@click="changeSort(column)">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t(column.label) }}</span>
						<template v-if="column.sortable">
							<template v-if="sort.key === column.key">
								<template v-if="sort.order === 'asc'">
									<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
								</template>
								<template v-else>
									<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
								</template>
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
							</template>
						</template>
					</div>
				</th>
			</tr>
		</thead>
		<tbody :class="tableauCss.tbody">
			<template v-if="meta?.path">
				<RouterLink v-for="row in sortedData" :key="row[meta.key]" :to="meta.path + row[meta.key]"
					custom v-slot="{ navigate }">
					<tr @click="navigate" :class="tableauCss.tr">
						<TableauRow
							:labels="labels"
							:row="row"
							:store-data="storeData"
							:css="tableauCss.td"
						/>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<tr v-for="row in sortedData" :key="row[meta.key]"
					:class="tableauCss.tr">
					<TableauRow
						:labels="labels"
						:row="row"
						:store-data="storeData"
						:css="tableauCss.td"
					/>
				</tr>
			</template>
		</tbody>
	</table>
</template>
<script>
import TableauRow from "./TableauRow.vue";
export default {
	name: "Tableau",
	props: {
		labels: {
			type: Array,
			required: true,
			// labels for the columns, each object should have a key and label property
			// e.g. [{ key: 'name', label: 'Name', sortable: true }, { key: 'price', label: 'Price', sortable: true }]
		},
		meta: {
			type: Object,
			required: true,
			// meta object containing additional information like path for RouterLink
			// e.g. { path: '/product/', key: 'id' }
		},
		storeData: {
			type: Array,
			required: true,
			// storeData is an array of objects, each object should contain the data for a row
			// storeData[0] is the main data array
		},
		loading: {
			type: Boolean,
			default: false,
			// loading state for the table, used to show a loading spinner or message
		},
		tableauCss: {
			type: Object,
			required: false,
			// tableauCss is an object containing tailwind CSS classes for the table, thead, th, tbody, tr, and td
			default: () => ({
				table: "min-w-full border-collapse border border-gray-300",
				thead: "bg-gray-100",
				th: "border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative",
				tbody: "",
				tr: "transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer",
				td: "border border-gray-300 px-4 py-2 text-sm text-gray-700",
			}),
		},
	},
	components: {
		TableauRow,
	},
	computed: {
		sortedData() {
			if (this.sort.key) {
				return [...Object.values(this.storeData[0])].sort((a, b) => {
					if (this.sort.order === "asc") {
						return a[this.sort.key] > b[this.sort.key] ? 1 : -1;
					} else {
						return a[this.sort.key] < b[this.sort.key] ? 1 : -1;
					}
				});
			}
			return this.storeData[0];
		},
	},
	data() {
		return {
			sort: {
				key: "",
				order: "asc",
			},
		};
	},
	methods: {
		changeSort(column) {
			if (!column.sortable) {
				return;
			}
			if (this.sort.key === column.key) {
				this.sort.order = this.sort.order === "asc" ? "desc" : "asc";
			} else {
				this.sort.key = column.key;
				this.sort.order = "asc";
			}
		},
	},
};
</script>