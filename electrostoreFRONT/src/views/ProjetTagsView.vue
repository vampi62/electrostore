<script setup>
import { onMounted, ref } from "vue";

import { useProjetTagsStore } from "@/stores";
const projetTagsStore = useProjetTagsStore();

async function fetchAllData() {
	let offset = 0;
	const limit = 100;
	do {
		await projetTagsStore.getProjetTagByInterval(limit, offset);
		offset += limit;
	} while (offset < projetTagsStore.projetTagsTotalCount);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "nom_projet_tag", value: "", type: "text", label: "projetTag.VProjetTagsFilterName", compareMethod: "contain" },
	{ key: "poids_projet_tag", value: "", type: "number", label: "projetTag.VProjetTagsFilterWeightMin", compareMethod: ">=" },
	{ key: "poids_projet_tag", value: "", type: "number", label: "projetTag.VProjetTagsFilterWeightMax", compareMethod: "<=" },
]);
const tableauLabel = ref([
	{ label: "projetTag.VProjetTagsName", sortable: true, key: "nom_projet_tag", type: "text" },
	{ label: "projetTag.VProjetTagsWeight", sortable: true, key: "poids_projet_tag", type: "number" },
	{ label: "projetTag.VProjetTagsProjetsCount", sortable: true, key: "projets_projet_tags_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_projet_tag",
	path: "/projet-tags/",
});
const filteredProjetTags = ref([]);
const updateFilteredProjetTags = (newValue) => {
	filteredProjetTags.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projetTag.VProjetTagsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projet-tags/new'">
				{{ $t('projetTag.VProjetTagsAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetTagsStore.projetTags" @output-filter="updateFilteredProjetTags" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredProjetTags]"
		:loading="projetTagsStore.projetTagsLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
