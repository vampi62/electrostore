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
					:class="trClass(row)"
					@click="openRow(row)">
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

<script lang="ts">
import { defineAsyncComponent } from "vue";
import type { PropType } from "vue";
import { useRouter } from "vue-router";
import { debounce } from "lodash-es";
import { matchFilter } from "@/helpers";
import { useTabsStore } from "@/stores";
import type { TableauLabel, TableauMeta, TableauCss, TableauRowData } from "@/types/tableau";
import type { FetchFunction, FetchPage, ListFetchFunction } from "@/types/fetch";
import type { FilterLabel } from "@/types/filter";
import type { FormSchema } from "@/types/form";
import type { StateTree } from "pinia";

export default {
	name: "Tableau",
	setup() {
		// router of the tab displaying the table
		const router = useRouter();
		const tabsStore = useTabsStore();
		return { router, tabsStore };
	},
	props: {
		labels: {
			type: Array as PropType<TableauLabel[]>,
			required: true,
			// labels for the columns, each object should have a key and label property
			// e.g. [{ key: 'name', label: 'Name', sortable: true, type: "text" }, { key: 'price', label: 'Price', sortable: true, type: "number" }]
			// or [{ key: 'name_ressource', label: 'Name', sortable: false, keyStore: "id_ressource", store: "1", type: "text" }
			// if store is provided, it will be used to fetch the data from an other store in the props storeData and use the keyStore to get the resource and normal key to get the value to display
			// "store" cannot have the value "0"
			// other example :
			// { label: "item.VInventoryTags", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_item", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "name_tag" }] } },
			// this print a list of tags for each item, using the idStoreLink to get the tags from the storeData[1] and the idStoreRessource to get the item from the storeData[2], and ressourcePrint is an array of objects to print the tags, each object should have a type and key property
		},
		meta: {
			type: Object as PropType<TableauMeta>,
			required: true,
			// meta object containing additional information like path for RouterLink
			// e.g. { path: '/product/', key: 'id', sort: 'name', sortOrder: 'asc', preventClear: false, expand: ['category'], saveState: false, stateKey: 'productTable' }
			// path is the path to navigate when clicking on a row, it will be concatenated with the value of the key in the row to get the final path
			// newTab is a boolean, when true the path is opened in a new tab (or the tab already displaying it) instead of the current tab
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
			type: Array as PropType<StateTree[]>,
			required: true,
			// storeData is an array of objects, each object should contain the data for a row
			// storeData[0] is the main data array
		},
		storeEdition: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
			// storeEdition is an object containing the store to edit a resource
		},
		storeReady: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
			// storeReady is an object containing the ready state of the store for editing a resource
		},
		filters: {
			type: Array as PropType<FilterLabel[]>,
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
			type: Function as PropType<FetchFunction>,
			default: (): FetchPage => {
				return [0, false];
			},
			// fetchFunction is a function that will be called to fetch the data for the table, it should accept the parameters limit, offset, expand, filter, sort, and clear
			// e.g. (limit, offset, expand, filter, sort, clear) => { store.fetchData(limit, offset, expand, filter, sort, clear) }
		},
		listFetchFunction: {
			type: Array as PropType<ListFetchFunction[]>,
			default: () => [],
		},
		tableauCss: {
			type: Object as PropType<TableauCss>,
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
			type: Object as PropType<FormSchema>,
			required: false,
			// schema for vee-validate, used for validation of the form inputs in TableauRow
			default: undefined,
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
			return this.filters.map((f) => [f.value, f.partialValues]);
		},
		filteredData() {
			// Merge storeData[0] (saved data), storeReady (validated but not yet saved changes) and
			// storeEdition (in-progress, unvalidated drafts) into a single list without duplicate ids.
			// Filters are only ever evaluated against storeData[0]/storeReady values: rows that only
			// exist in storeEdition are unvalidated drafts and are always shown regardless of filters.
			const seen: Set<string> = new Set();
			const rows: TableauRowData[] = [];
			this.collectSavedRows(seen, rows);
			this.collectReadyOnlyRows(seen, rows);
			this.collectEditionOnlyRows(seen, rows);
			return rows;
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
					value: this.getDataValue(item, this.sort.key ?? ""),
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
					} else if (aValue > bValue) {
						comparison = 1;
					} else if (aValue < bValue) {
						comparison = -1;
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
	},
	data() {
		return {
			sort: {
				key: null as string | null,
				order: "asc" as "asc" | "desc",
			},
			nextOffset: 0,
			hasMore: true,
			isInitializing: true,
			debouncedRefetchData: null as (() => void) | null,
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
		[this.nextOffset, this.hasMore] = await this.fetchFunction(100, this.nextOffset, this.meta?.expand || [], this.filters, { key: this.sort.key ?? undefined, order: this.sort.order }, false);
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
					this.debouncedRefetchData?.();
				}
			},
		},
		sort: {
			handler() {
				if (!this.isInitializing) {
					this.debouncedRefetchData?.();
				}
			},
			deep: true,
		},
	},
	methods: {
		openRow(row: TableauRowData) {
			if (!this.meta?.path) {
				return;
			}
			const path = this.meta.path + row[this.meta.key];
			if (this.meta.newTab) {
				this.tabsStore.open(path);
			} else {
				this.router.push(path);
			}
		},
		_sessionStateKey() {
			return `tableau_state_${this.$route?.path}_${this.meta?.stateKey || this.meta?.path + this.meta?.key || "default"}`;
		},
		_saveState(updates: Record<string, unknown>) {
			const current = JSON.parse(sessionStorage.getItem(this._sessionStateKey()) || "{}");
			sessionStorage.setItem(this._sessionStateKey(), JSON.stringify({ ...current, ...updates }));
		},
		rowStatusClass(row: TableauRowData) {
			// Colors are driven by the ready entry's own status rather than by "is this id also in
			// storeData[0]": some tables (e.g. a picker browsing a full catalog) always have the id in
			// storeData[0] regardless of whether it's actually linked/pending, so presence alone can't
			// tell "unchanged" from "pending change" there - the status value can.
			const id = row[this.meta.key];
			const readyEntry = this.storeReady?.[id];
			if (readyEntry) {
				switch (readyEntry.status) {
				case "deleted": return "bg-red-100 text-red-800 hover:bg-red-200";
				case "created": return "bg-green-100 text-green-800 hover:bg-green-200";
				case "modified": return "bg-amber-100 text-amber-800 hover:bg-amber-200";
				}
				return "";
			}
			if (row?.deleted_at !== null && row?.deleted_at !== undefined) {
				return "bg-red-100 text-red-800 hover:bg-red-200";
			}
			const inStoreData = Object.hasOwn(this.storeData[0] || {}, id);
			if (!inStoreData && this.storeEdition?.[id]) {
				return "bg-green-100 text-green-800 hover:bg-green-200";
			}
			return "";
		},
		trClass(row: TableauRowData) {
			const statusClass = this.rowStatusClass(row);
			if (!statusClass) {
				return this.mergedCss.tr;
			}
			const baseWithoutBackground = this.mergedCss.tr
				.split(/\s+/)
				.filter((cls: string) => cls && !cls.includes("bg-"))
				.join(" ");
			return `${baseWithoutBackground} ${statusClass}`.trim();
		},
		extractReadyFields(readyEntry: StateTree) {
			// storeReady entries carry bookkeeping fields (status, isFormData, pushChange) alongside
			// the actual resource fields, only the resource fields are relevant for display/filtering.
			const { status, isFormData, pushChange, ...data } = readyEntry;
			return data;
		},
		collectSavedRows(seen: Set<string>, rows: TableauRowData[]) {
			if (!this.storeData[0]) {
				return;
			}
			for (const [id, row] of Object.entries<TableauRowData>(this.storeData[0])) {
				seen.add(id);
				const readyEntry = this.storeReady?.[id];
				const merged = readyEntry ? { ...row, ...this.extractReadyFields(readyEntry) } : row;
				if (this.matchesFilters(merged)) {
					rows.push(merged);
				}
			}
		},
		collectReadyOnlyRows(seen: Set<string>, rows: TableauRowData[]) {
			if (!this.storeReady) {
				return;
			}
			for (const [id, readyEntry] of Object.entries<StateTree>(this.storeReady)) {
				if (seen.has(id)) {
					continue;
				}
				seen.add(id);
				const merged = { [this.meta.key]: id, ...this.extractReadyFields(readyEntry) };
				if (this.matchesFilters(merged)) {
					rows.push(merged);
				}
			}
		},
		collectEditionOnlyRows(seen: Set<string>, rows: TableauRowData[]) {
			if (!this.storeEdition) {
				return;
			}
			for (const [id, edition] of Object.entries<StateTree>(this.storeEdition)) {
				if (seen.has(id)) {
					continue;
				}
				seen.add(id);
				rows.push({ [this.meta.key]: id, ...edition });
			}
		},
		matchesFilters(element: TableauRowData) {
			return this.filters.every((filter) => matchFilter(filter, () => this.getDataValue(element, filter.key)));
		},
		getDataValue(row: TableauRowData, labelKey: string) {
			const label = this.labels.find((l) => l.key === labelKey);
			if (!label) {
				return row?.[labelKey];
			}
			switch (label.type) {
			case "link-list":
				return this.getLinkListValue(row, label);
			case "image":
				return this.getImageValue(row, label);
			case "buttons":
				return this.getButtonsValue(row, label);
			case "bool":
				return this.evaluateCondition(label.condition ?? "false", row) || false;
			}
			return this.getRessourceValue(row, label);
		},
		getLinkListValue(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, storeRessourceId, sourceKey, storeLinkKeyJoinRessource, ressourcePrint } = label;
			if (storeLinkId === undefined || sourceKey === undefined) {
				return [];
			}
			return Object.values<TableauRowData>(this.storeData[storeLinkId]?.[row[sourceKey]] || {}).map((linkedItem) => {
				let printedRessource = "";
				for (const print of ressourcePrint ?? []) {
					if (print.from === "ressource" && storeRessourceId && storeLinkKeyJoinRessource && print.valueKey) {
						printedRessource += this.storeData[storeRessourceId]?.[linkedItem[storeLinkKeyJoinRessource]]?.[print.valueKey] || "";
					} else if (print.from === "link" && print.valueKey) {
						printedRessource += linkedItem?.[print.valueKey] || "";
					} else if (print.from === "text") {
						printedRessource += print.text || "";
					}
				}
				return printedRessource;
			});
		},
		getImageValue(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, storeRessourceId, sourceKey, valueKey, storeLinkKeyJoinRessource } = label;
			if (storeRessourceId === undefined || sourceKey === undefined) {
				return undefined;
			}
			const ressources = this.storeData[storeRessourceId];
			const linkedItem = storeLinkId && storeLinkKeyJoinRessource ? this.storeData[storeLinkId]?.[row[sourceKey]] : undefined;
			const ressource = linkedItem ? ressources?.[linkedItem[storeLinkKeyJoinRessource!]] : ressources?.[row[sourceKey]];
			return valueKey ? ressource?.[valueKey] : ressource;
		},
		getButtonsValue(row: TableauRowData, label: TableauLabel) {
			return (label.buttons ?? []).map((button) => ({
				...button,
				show: button?.showCondition ? this.evaluateCondition(button.showCondition, row) : true,
				enable: button?.enableCondition ? this.evaluateCondition(button.enableCondition, row) : true,
			}));
		},
		getRessourceValue(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, storeRessourceId, sourceKey, valueKey, storeLinkKeyJoinRessource } = label;
			if (!storeRessourceId) {
				return valueKey === undefined ? undefined : row?.[valueKey];
			}
			if (sourceKey === undefined || valueKey === undefined) {
				return undefined;
			}
			if (!storeLinkId) {
				return this.storeData[storeRessourceId]?.[row[sourceKey]]?.[valueKey];
			}
			if (storeLinkKeyJoinRessource === undefined) {
				return undefined;
			}
			const linkedItem = this.storeData[storeLinkId]?.[row[sourceKey]];
			return this.storeData[storeRessourceId]?.[linkedItem?.[storeLinkKeyJoinRessource]]?.[valueKey];
		},
		evaluateCondition(condition: string, rowData: TableauRowData) {
			try {
				return new Function("store", "rowData", `return ${condition}`)(this.storeData,rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		changeSort(key: string) {
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
		async loadNext(e: Event) {
			const target = e.target as HTMLElement;
			if (this.meta?.saveState) {
				this._saveState({ scrollTop: target.scrollTop });
			}
			if (this.totalCount === 0 || this.loading || !this.hasMore) {
				return;
			}
			if (target.scrollTop + target.clientHeight >= target.scrollHeight - 10) {
				if (this.totalCount === this.nextOffset) {
					return;
				}
				let intervalOffset = this.nextOffset;
				[this.nextOffset, this.hasMore] = await this.fetchFunction(100, this.nextOffset, this.meta?.expand || [], this.filters, { key: this.sort.key ?? undefined, order: this.sort.order }, false);
				await this.refetchListData(intervalOffset, this.nextOffset);
			}
		},
		async refetchData() {
			// Reset to initial state before refetching
			this.nextOffset = 0;
			this.hasMore = true;
			let intervalOffset = this.nextOffset;
			[this.nextOffset, this.hasMore] = await this.fetchFunction(100, 0, this.meta?.expand || [], this.filters, { key: this.sort.key ?? undefined, order: this.sort.order }, !this.meta?.preventClear);
			await this.refetchListData(intervalOffset, this.nextOffset);
		},
		async refetchListData(minOffset: number, maxOffset: number) {
			for (const fetchFn of this.listFetchFunction) {
				if (fetchFn) {
					await fetchFn(minOffset, maxOffset);
				}
			}
		},
	},
};
</script>
