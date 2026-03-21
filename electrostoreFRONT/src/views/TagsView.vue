<script setup>
import { ref } from "vue";

import { useTagsStore } from "@/stores";
const tagsStore = useTagsStore();

const filter = ref([
	{ key: "nom_tag", value: "", type: "text", label: "tags.FilterName", compareMethod: "=like=" },
	{ key: "poids_tag", value: "", type: "number", label: "tags.FilterWeightMin", compareMethod: "=ge=" },
	{ key: "poids_tag", value: "", type: "number", label: "tags.FilterWeightMax", compareMethod: "=le=" },
]);
const tableauLabel = ref([
	{ label: "tags.Name", sortable: true, key: "nom_tag", valueKey: "nom_tag", type: "text" },
	{ label: "tags.Weight", sortable: true, key: "poids_tag", valueKey: "poids_tag", type: "number" },
	{ label: "tags.ItemsCount", sortable: true, key: "ItemsTags.Count", valueKey: "items_tags_count", type: "number" },
	{ label: "tags.StoresCount", sortable: true, key: "StoresTags.Count", valueKey: "stores_tags_count", type: "number" },
	{ label: "tags.BoxsCount", sortable: true, key: "BoxsTags.Count", valueKey: "boxs_tags_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_tag",
	path: "/tags/",
	saveState: true,
	stateKey: "tagsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('tags.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/tags/new'">
				{{ $t('tags.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="tagsStore.tags" @ready="filterReady = true" :save-state="true" state-key="tagsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[tagsStore.tags]"
		:filters="filter"
		:loading="tagsStore.tagsLoading"
		:total-count="Number(tagsStore.tagsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => tagsStore.getTagByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
