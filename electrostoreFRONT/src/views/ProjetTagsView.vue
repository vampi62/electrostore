<script setup>
import { ref } from "vue";

import { useProjetTagsStore } from "@/stores";
const projetTagsStore = useProjetTagsStore();

const filter = ref([
	{ key: "nom_projet_tag", value: "", type: "text", label: "projetTags.FilterName", compareMethod: "=like=" },
	{ key: "poids_projet_tag", value: "", type: "number", label: "projetTags.FilterWeightMin", compareMethod: "=ge=" },
	{ key: "poids_projet_tag", value: "", type: "number", label: "projetTags.FilterWeightMax", compareMethod: "=le=" },
]);
const tableauLabel = ref([
	{ label: "projetTags.Name", sortable: true, key: "nom_projet_tag", valueKey: "nom_projet_tag", type: "text" },
	{ label: "projetTags.Weight", sortable: true, key: "poids_projet_tag", valueKey: "poids_projet_tag", type: "number" },
	{ label: "projetTags.ProjetsCount", sortable: true, key: "ProjetsProjetTags.Count", valueKey: "projets_projet_tags_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_projet_tag",
	path: "/projet-tags/",
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projetTags.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projet-tags/new'">
				{{ $t('projetTags.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetTagsStore.projetTags" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projetTagsStore.projetTags]"
		:filters="filter"
		:loading="projetTagsStore.projetTagsLoading"
		:total-count="Number(projetTagsStore.projetTagsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
