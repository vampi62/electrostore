<template>
	<div class="overflow-x-auto overflow-y-auto" :class="mergedCss.component" @scroll="loadNext">
		<table :class="mergedCss.table">
			<thead :class="mergedCss.thead">
				<tr>
					<th v-for="(column,index) in labels"
						:key="index"
						:class="[mergedCss.th, column.sortable ? 'cursor-pointer' : '']"
						@click="changeSort(column)">
						<div class="flex justify-between items-center">
							<span class="flex-1">{{ $t(column.label) }}</span>
							<template v-if="column.sortable">
								<template v-if="sort.column?.key === column.key">
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
			<tbody :class="mergedCss.tbody">
				<template v-if="meta?.path">
					<RouterLink v-for="row in sortedData" :key="row[meta.key]" :to="meta.path + row[meta.key]"
						custom v-slot="{ navigate }">
						<tr @click="navigate" :class="mergedCss.tr">
							<TableauRow
								:labels="labels"
								:row="row"
								:schema="schema"
								:store-data="storeData"
								:css="mergedCss.td"
							/>
						</tr>
					</RouterLink>
				</template>
				<template v-else>
					<tr v-for="row in sortedData" :key="row[meta.key]"
						:class="mergedCss.tr">
						<TableauRow
							:labels="labels"
							:row="row"
							:schema="schema"
							:store-data="storeData"
							:css="mergedCss.td"
						/>
					</tr>
				</template>
				<slot name="append-row"></slot>
				<template v-if="loading">
					<tr>
						<td :colspan="labels.length" class="text-center py-4">
							<font-awesome-icon icon="fa-solid fa-spinner" spin class="text-gray-500" />
						</td>
					</tr>
				</template>
			</tbody>
		</table>
	</div>
</template>

<script>
import { defineAsyncComponent } from "vue";
import { debounce } from "lodash-es";
import { buildRSQLFilter, buildRSQLSort } from "@/utils";
import { toLowerCaseWithoutAccents } from "@/utils";
export default {
	name: "Tableau",
	props: {
		labels: {
			type: Array,
			required: true,
			// labels for the columns, each object should have a key and label property
			// e.g. [{ key: 'name', label: 'Name', sortable: true, type: "text" }, { key: 'price', label: 'Price', sortable: true, type: "number" }]
			// or [{ key: 'name_ressource', label: 'Name', sortable: false, keyStore: "id_ressource", store: "1", type: "text" }
			// if store is provided, it will be used to fetch the data from an other store in the props storeData and use the keyStore to get the resource and normal key to get the value to display
			// "store" cannot have the value "0"
			// other example :
			// { label: "item.VInventoryTags", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_item", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "nom_tag" }] } },
			// this print a list of tags for each item, using the idStoreLink to get the tags from the storeData[1] and the idStoreRessource to get the item from the storeData[2], and ressourcePrint is an array of objects to print the tags, each object should have a type and key property
		},
		meta: {
			type: Object,
			required: true,
			// meta object containing additional information like path for RouterLink
			// e.g. { path: '/product/', key: 'id', sort: 'name', sortOrder: 'asc', preventClear: false }
		},
		storeData: {
			type: Array,
			required: true,
			// storeData is an array of objects, each object should contain the data for a row
			// storeData[0] is the main data array
		},
		filters: {
			type: Array,
			required: false,
			// filters is an array of filter objects, each object should have a key, value, type, dataType, compareMethod, and optional subPath, placeholder, class, and options properties
			// e.g. { key: 'name', value: '', type: 'text', dataType: 'string', compareMethod: 'contain', placeholder: 'Search by name', class: 'mb-2' }
		},
		loading: {
			type: Boolean,
			default: false,
			// loading state for the table, used to show a loading spinner or message
		},
		totalCount: {
			type: Number,
			default: 0,
		},
		fetchFunction: {
			type: Function,
			default: () => {},
		},
		tableauCss: {
			type: Object,
			required: false,
			// tableauCss is an object containing tailwind CSS classes for the table, thead, th, tbody, tr, and td
			default: () => ({
				component: "max-h-64",
				table: "min-w-full table-auto",
				thead: "bg-gray-300 sticky top-0",
				th: "px-4 py-2 text-center",
				tbody: "",
				tr: "transition duration-150 ease-in-out cursor-pointer hover:bg-gray-200 even:bg-gray-100",
				td: "border-b",
			}),
		},
		schema: {
			type: Object,
			required: false,
			// schema for vee-validate, used for validation of the form inputs in TableauRow
			default: () => ({}),
		},
	},
	components: {
		TableauRow: defineAsyncComponent(() => import("@/components/TableauRow.vue")),
	},
	computed: {
		filteredData() {
			return Object.values(this.storeData[0]).filter((element) => {
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
								return Number.parseInt(element[f.key]) === Number.parseInt(f.value);
							case "float":
								return Number.parseFloat(element[f.key]) === Number.parseFloat(f.value);
							case "string":
								//return element[f.key].toLowerCase() === f.value.toLowerCase();
								return toLowerCaseWithoutAccents(element[f.key]) === toLowerCaseWithoutAccents(f.value);
							}
							return element[f.key] === f.value;
						case ">=":
							return element[f.key] >= f.value;
						case "<=":
							return element[f.key] <= f.value;
						case "contain":
							return toLowerCaseWithoutAccents(element[f.key]).includes(toLowerCaseWithoutAccents(f.value));
						}
					}
					return true;
				});
			});
		},
		sortedData() {
			if (this.sort.column) {
				return [...Object.values(this.filteredData)].sort((a, b) => {
					if (this.sort.column?.store) {
						if (this.sort.order === "asc") {
							return this.storeData[this.sort.column.store][a[this.sort.column.keyStore]]?.[this.sort.column.key] > this.storeData[this.sort.column.store][b[this.sort.column.keyStore]]?.[this.sort.column.key] ? 1 : -1;
						} else {
							return this.storeData[this.sort.column.store][a[this.sort.column.keyStore]]?.[this.sort.column.key] < this.storeData[this.sort.column.store][b[this.sort.column.keyStore]]?.[this.sort.column.key] ? 1 : -1;
						}
					}
					if (this.sort.order === "asc") {
						return a[this.sort.column.key] > b[this.sort.column.key] ? 1 : -1;
					} else {
						return a[this.sort.column.key] < b[this.sort.column.key] ? 1 : -1;
					}
				});
			}
			return this.filteredData;
		},
		mergedCss() {
			// Merges the default CSS with the provided tableauCss prop
			return {
				component: this.tableauCss?.component || "min-h-64 max-h-64",
				table: this.tableauCss?.table || "min-w-full table-auto",
				thead: this.tableauCss?.thead || "bg-gray-300 sticky top-0",
				th: this.tableauCss?.th || "px-4 py-2 text-center",
				tbody: this.tableauCss?.tbody || "",
				tr: this.tableauCss?.tr || "transition duration-150 ease-in-out cursor-pointer hover:bg-gray-200 even:bg-gray-100",
				td: this.tableauCss?.td || "border-b",
			};
		},
	},
	data() {
		return {
			sort: {
				column: null,
				order: "asc",
			},
			nextOffset: 0,
			hasMore: true,
			isInitializing: true,
		};
	},
	created() {
		this.debouncedRefetchData = debounce(this.refetchData, 500);
	},
	async mounted() {
		if (this.meta?.sort) {
			this.sort.column = this.labels.find((col) => col.key === this.meta.sort);
			this.sort.order = this.meta.sortOrder || "asc";
		}
		[this.nextOffset, this.hasMore] = await this.fetchFunction(0, 100, [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort));
		this.isInitializing = false;
	},
	watch: {
		filters: {
			handler() {
				if (!this.isInitializing) {
					this.debouncedRefetchData();
				}
			},
			deep: true,
		},
		sort: {
			handler() {
				if (!this.isInitializing) {
					this.debouncedRefetchData();
				}
			},
			deep: true,
		},
	},
	methods: {
		changeSort(column) {
			if (!column.sortable) {
				return;
			}
			if (this.sort.column === column) {
				this.sort.order = this.sort.order === "asc" ? "desc" : "asc";
			} else {
				this.sort.column = column;
				this.sort.order = "asc";
			}
		},
		async loadNext(e) {
			if (this.totalCount === 0 || this.loading || !this.hasMore) {
				return;
			}
			if (e.target.scrollTop + e.target.clientHeight >= e.target.scrollHeight - 10) {
				if (this.totalCount === this.nextOffset) {
					return;
				}
				[this.nextOffset, this.hasMore] = await this.fetchFunction(this.nextOffset, 100, [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort));
			}
		},
		async refetchData() {
			// Reset l'état et refetch les données depuis le début
			this.nextOffset = 0;
			this.hasMore = true;
			[this.nextOffset, this.hasMore] = await this.fetchFunction(0, 100, [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort), this.meta?.preventClear ? false : true);
		},
	},
};
</script>