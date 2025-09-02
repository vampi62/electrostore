<script setup>
import { onMounted, ref } from "vue";

import { useTagsStore } from "@/stores";
const tagsStore = useTagsStore();

async function fetchAllData() {
	let offset = 0;
	const limit = 100;
	do {
		await tagsStore.getTagByInterval(limit, offset);
		offset += limit;
	} while (offset < tagsStore.tagsTotalCount);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "nom_tag", value: "", type: "text", label: "tag.VTagsFilterName", compareMethod: "contain" },
	{ key: "poids_tag", value: "", type: "number", label: "tag.VTagsFilterWeightMin", compareMethod: ">=" },
	{ key: "poids_tag", value: "", type: "number", label: "tag.VTagsFilterWeightMax", compareMethod: "<=" },
]);
const tableauLabel = ref([
	{ label: "tag.VTagsName", sortable: true, key: "nom_tag", type: "text" },
	{ label: "tag.VTagsWeight", sortable: true, key: "poids_tag", type: "number" },
	{ label: "tag.VTagsItemsCount", sortable: true, key: "items_tags_count", type: "number" },
	{ label: "tag.VTagsStoresCount", sortable: true, key: "stores_tags_count", type: "number" },
	{ label: "tag.VTagsBoxsCount", sortable: true, key: "boxs_tags_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_tag",
	path: "/tags/",
});
const filteredTags = ref([]);
const updateFilteredTags = (newValue) => {
	filteredTags.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('tag.VTagsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/tags/new'">
				{{ $t('tag.VTagsAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="tagsStore.tags" @output-filter="updateFilteredTags" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredTags]"
		:loading="tagsStore.tagsLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
