<template>
	<div class="overflow-x-auto overflow-y-auto" :class="mergedCss.component" @scroll="loadNext">
		<table :class="mergedCss.table">
			<thead :class="mergedCss.thead">
				<tr>
					<th v-for="(column,index) in labelsShown"
						:key="index"
						:class="[mergedCss.th, column.sortable ? 'cursor-pointer' : '']"
						@click="column.sortable && changeSort(column.key)">
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
			<tbody :class="mergedCss.tbody">
				<tr v-for="row in sortedData" :key="row[meta.key]" v-memo="[row, storeEdition[row[meta.key]], storeReady[row[meta.key]]]"
					:class="mergedCss.tr"
					@click="meta?.path && $router.push(meta.path + row[meta.key])">
					<TableauRow :labels="labelsShown" :row="row" :css="mergedCss.td" :schema="schema" :store-data="storeData" :store-edition="storeEdition[row[meta.key]]" :store-ready="storeReady[row[meta.key]]" />
				</tr>
				<tr v-for="row in filterMissingEdition" :key="'edition-' + row[meta.key]"
					:class="[mergedCss.tr, 'bg-yellow-100']">
					<TableauRow :labels="labelsShown" :row="row" :css="mergedCss.td" :schema="schema" :store-data="storeData" :store-edition="storeEdition[row[meta.key]]" :store-ready="storeReady[row[meta.key]]" />
				</tr>
				<tr v-for="row in filterMissingReady" :key="'ready-' + row[meta.key]"
					:class="[mergedCss.tr, 'bg-red-100']">
					<TableauRow :labels="labelsShown" :row="row" :css="mergedCss.td" :schema="schema" :store-data="storeData" :store-edition="storeEdition[row[meta.key]]" :store-ready="storeReady[row[meta.key]]" />
				</tr>
				<slot name="append-row"></slot>
				<template v-if="loading">
					<tr>
						<td :colspan="labelsShown.length" class="text-center py-4">
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
import { buildRSQLFilter, buildRSQLSort, toLowerCaseWithoutAccents } from "@/utils";
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
			// e.g. { path: '/product/', key: 'id', sort: 'name', sortOrder: 'asc', preventClear: false, expand: ['category'], saveState: false, stateKey: 'productTable' }
			// path is the path to navigate when clicking on a row, it will be concatenated with the value of the key in the row to get the final path
			// key is the key in the row to concatenate with the path for navigation
			// sort is the default sort key
			// sortOrder is the default sort order, can be 'asc' or 'desc'
			// preventClear is a boolean to prevent clearing the data when refetching, useful for infinite scroll
			// expand is an array of strings to pass to the fetchFunction for expanding resources
			// saveState is a boolean to save the scroll and sort state in sessionStorage
			// stateKey is a string to identify the state in sessionStorage, required if saveState is true
			// linkEditionKey is a string to identify the key to link edition and ready stores to the main store, if not provided it will use the main key
		},
		storeData: {
			type: Array,
			required: true,
			// storeData is an array of objects, each object should contain the data for a row
			// storeData[0] is the main data array
		},
		storeEdition: {
			type: Object,
			required: false,
			default: () => ({}),
			// storeEdition is an object containing the store to edit a resource
		},
		storeReady: {
			type: Object,
			required: false,
			default: () => ({}),
			// storeReady is an object containing the ready state of the store for editing a resource
		},
		filters: {
			type: Array,
			required: false,
			// filters is an array of filter objects, each object should have a key, value, type, typeData, compareMethod, placeholder, class, and options properties
			// e.g. { key: 'name', value: '', type: 'text', typeData: 'string', compareMethod: 'contain', placeholder: 'Search by name', class: 'mb-2' }
			default: () => [],
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
			default: (limit, offset, expand, filter, sort, clear) => { 
				return [0, false];
			},
			// fetchFunction is a function that will be called to fetch the data for the table, it should accept the parameters limit, offset, expand, filter, sort, and clear
			// e.g. (limit, offset, expand, filter, sort, clear) => { store.fetchData(limit, offset, expand, filter, sort, clear) }
		},
		listFetchFunction: {
			type: Array,
			default: () => [],
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
		labelsShown() {
			return this.labels.filter((label) => {
				if (label.showCondition === undefined) {
					return true;
				}
				return eval(label.showCondition);
			});
		},
		filterValues() {
			return this.filters.map((f) => f.value);
		},
		filteredData() {
			if (!this.storeData[0]) {
				return [];
			}
			if (!this.filters || this.filters.length === 0) {
				return Object.values(this.storeData[0]);
			}
			return Object.values(this.storeData[0]).filter((element) => {
				return this.filters.every((f) => {
					if (f.value === "" || f.value === null || f.value === undefined || f.disableLocalFilter) {
						return true;
					}
					const elementValue = this.getDataValue(element, f.key);
					switch (f.compareMethod) {
					case "==":
						switch (f?.typeData || f.type) {
						case "bool":
							return elementValue === (f.value === "true");
						case "number":
							return Number.parseInt(elementValue) === Number.parseInt(f.value);
						case "string":
							if (Array.isArray(elementValue)) {
								return elementValue.some((item) => 
									toLowerCaseWithoutAccents(String(item)).includes(toLowerCaseWithoutAccents(f.value)),
								);
							}
							return toLowerCaseWithoutAccents(elementValue) === toLowerCaseWithoutAccents(f.value);
						}
						return elementValue === f.value;
					case "=ge=":
						return elementValue >= f.value;
					case "=le=":
						return elementValue <= f.value;
					case "=like=":
						if (Array.isArray(elementValue)) {
							return elementValue.some((item) => 
								toLowerCaseWithoutAccents(String(item)).includes(toLowerCaseWithoutAccents(f.value)),
							);
						}
						return toLowerCaseWithoutAccents(elementValue).includes(toLowerCaseWithoutAccents(f.value));
					}
				});
			});
		},
		sortedData() {
			if (!this.sort.key) {
				return this.filteredData;
			}
			const sortOrder = this.sort.order === "asc" ? 1 : -1;
			// Transformation de Schwartzian : pré-calculer les valeurs une seule fois
			return this.filteredData
				.map((item) => ({
					item,
					value: this.getDataValue(item, this.sort.key),
				}))
				.sort((a, b) => {
					const aValue = a.value;
					const bValue = b.value;
					
					// Gérer les valeurs undefined
					if (aValue === undefined && bValue === undefined) {
						return 0;
					}
					if (aValue === undefined) {
						return 1;
					}
					if (bValue === undefined) {
						return -1;
					}
					
					// Comparaison typée appropriée
					let comparison = 0;
					if (typeof aValue === "string" && typeof bValue === "string") {
						comparison = aValue.localeCompare(bValue, undefined, { numeric: true });
					} else if (typeof aValue === "number" && typeof bValue === "number") {
						comparison = aValue - bValue;
					} else {
						comparison = aValue > bValue ? 1 : (aValue < bValue ? -1 : 0);
					}
					
					return comparison * sortOrder;
				})
				.map((decorated) => decorated.item);
		},
		mergedCss() {
			// Merges the default CSS with the provided tableauCss prop
			return {
				component: this.tableauCss?.component || "min-h-48 max-h-48 md:min-h-64 sm:max-h-64",
				table: this.tableauCss?.table || "min-w-full table-auto text-xs md:text-sm",
				thead: this.tableauCss?.thead || "bg-gray-300 sticky top-0",
				th: this.tableauCss?.th || "px-2 py-1 md:px-4 md:py-2 text-center",
				tbody: this.tableauCss?.tbody || "",
				tr: this.tableauCss?.tr || "transition duration-150 ease-in-out cursor-pointer hover:bg-gray-200 even:bg-gray-100",
				td: this.tableauCss?.td || "border-b px-2 py-1 md:px-4 md:py-2",
			};
		},
		filterMissingEdition() {
			if (!this.storeEdition) {
				return [];
			}
			return Object.values(this.storeEdition).filter((edition) => {
				return !this.storeData[0] || !this.storeData[0][edition[this.meta.key]];
			});
		},
		filterMissingReady() {
			if (!this.storeReady) {
				return [];
			}
			return Object.values(this.storeReady).filter((ready) => {
				if (!this.storeData[0]) {
					return true;
				}
				return Object.values(this.storeData[0]).some((data) => data[this.meta.linkEditionKey || this.meta.key] === ready.data[this.meta.key]);
			})
				.map((ready) => ready.data);
		},
	},
	data() {
		return {
			sort: {
				key: null,
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
			this.sort.key = this.meta.sort;
			this.sort.order = this.meta.sortOrder === "desc" ? "desc" : "asc";
		}
		let savedScrollTop = 0;
		if (this.meta?.saveState) {
			const saved = sessionStorage.getItem(this._sessionStateKey());
			if (saved) {
				const state = JSON.parse(saved);
				if (state.sort?.key) {
					this.sort.key = state.sort.key;
					this.sort.order = state.sort.order === "desc" ? "desc" : "asc";
				}
				savedScrollTop = state.scrollTop || 0;
			}
		}
		let intervalOffset = this.nextOffset;
		[this.nextOffset, this.hasMore] = await this.fetchFunction(100, this.nextOffset, this.meta?.expand || [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort));
		await this.refetchListData(intervalOffset, this.nextOffset);
		this.isInitializing = false;
		if (this.meta?.saveState && savedScrollTop > 0) {
			await this.$nextTick();
			this.$el.scrollTop = savedScrollTop;
		}
	},
	watch: {
		filterValues: {
			handler() {
				if (!this.isInitializing) {
					this.debouncedRefetchData();
				}
			},
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
		_sessionStateKey() {
			return `tableau_state_${this.$route?.path}_${this.meta?.stateKey || this.meta?.path + this.meta?.key || "default"}`;
		},
		_saveState(updates) {
			const current = JSON.parse(sessionStorage.getItem(this._sessionStateKey()) || "{}");
			sessionStorage.setItem(this._sessionStateKey(), JSON.stringify({ ...current, ...updates }));
		},
		getDataValue(row, labelKey) {
			const label = this.labels.find((l) => l.key === labelKey);
			if (!label) {
				return row?.[labelKey];
			}
			if (label.type === "link-list") {
				return Object.values(this.storeData[label.storeLinkId]?.[row[label.sourceKey]] || {}).map((linkedItem) => {
					let printedRessource = "";
					label.ressourcePrint.forEach((print) => {
						if (print.from === "ressource") {
							printedRessource += this.storeData[label.storeRessourceId]?.[linkedItem[label.storeLinkKeyJoinRessource]]?.[print.valueKey] || "";
						} else if (print.from === "link") {
							printedRessource += linkedItem?.[print.valueKey] || "";
						} else if (print.from === "text") {
							printedRessource += print.text || "";
						}
					});
					return printedRessource;
				});
			} else if (label.type === "image") {
				if (label.storeLinkId && label.storeRessourceId) {
					const linkedItem = this.storeData[label.storeLinkId]?.[row[label.sourceKey]];
					if (linkedItem) {
						return this.storeData[label.storeRessourceId]?.[linkedItem[label.storeLinkKeyJoinRessource]]?.[label.valueKey];
					}
				}
				return this.storeData[label.storeRessourceId]?.[row[label.sourceKey]]?.[label.valueKey];
			} else if (label.type === "buttons") {
				return label.buttons.map((button) => {
					return {
						...button,
						show: button?.showCondition ? this.evaluateCondition(button.showCondition, row) : true,
						enable: button?.enableCondition ? this.evaluateCondition(button.enableCondition, row) : true,
					};
				});
			} else if (label.type === "bool") {
				return this.evaluateCondition(label.condition, row) || false;
			} else if (label.storeRessourceId && label.storeLinkId) {
				const linkedItem = this.storeData[label.storeLinkId]?.[row[label.sourceKey]];
				return this.storeData[label.storeRessourceId]?.[linkedItem?.[label.storeLinkKeyJoinRessource]]?.[label.valueKey];
			} else if (label.storeRessourceId && !label.storeLinkId) {
				return this.storeData[label.storeRessourceId]?.[row[label.sourceKey]]?.[label.valueKey];
			} else {
				return row?.[label.valueKey];
			}
		},
		evaluateCondition(condition,rowData) {
			try {
				return new Function(["store","rowData"], `return ${condition}`)(this.storeData,rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		changeSort(key) {
			if (this.sort.key === key) {
				this.sort.order = this.sort.order === "asc" ? "desc" : "asc";
			} else {
				this.sort.key = key;
				this.sort.order = "asc";
			}
			if (this.meta?.saveState) {
				this._saveState({ sort: { key: this.sort.key, order: this.sort.order }, scrollTop: 0 });
			}
		},
		async loadNext(e) {
			if (this.meta?.saveState) {
				this._saveState({ scrollTop: e.target.scrollTop });
			}
			if (this.totalCount === 0 || this.loading || !this.hasMore) {
				return;
			}
			if (e.target.scrollTop + e.target.clientHeight >= e.target.scrollHeight - 10) {
				if (this.totalCount === this.nextOffset) {
					return;
				}
				let intervalOffset = this.nextOffset;
				[this.nextOffset, this.hasMore] = await this.fetchFunction(100, this.nextOffset, this.meta?.expand || [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort));
				await this.refetchListData(intervalOffset, this.nextOffset);
			}
		},
		async refetchData() {
			// Reset to initial state before refetching
			this.nextOffset = 0;
			this.hasMore = true;
			let intervalOffset = this.nextOffset;
			[this.nextOffset, this.hasMore] = await this.fetchFunction(100, 0, this.meta?.expand || [], buildRSQLFilter(this.filters), buildRSQLSort(this.sort), this.meta?.preventClear ? false : true);
			await this.refetchListData(intervalOffset, this.nextOffset);
		},
		async refetchListData(minOffset, maxOffset) {
			for (const fetchFn of this.listFetchFunction) {
				if (fetchFn) {
					await fetchFn(minOffset, maxOffset);
				}
			}
		},
	},
};
</script>