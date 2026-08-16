<script setup>
import { ref } from "vue";

import { useProjectTagsStore } from "@/stores";
const projetTagsStore = useProjectTagsStore();

const filter = ref([
	{ key: "name_project_tag", value: "", type: "text", label: "projectTags.FilterName", compareMethod: "=like=" },
	{ key: "weight_project_tag", value: "", type: "number", label: "projectTags.FilterWeightMin", compareMethod: "=ge=" },
	{ key: "weight_project_tag", value: "", type: "number", label: "projectTags.FilterWeightMax", compareMethod: "=le=" },
]);
const tableauLabel = ref([
	{ label: "projectTags.Name", sortable: true, key: "name_project_tag", valueKey: "name_project_tag", type: "text" },
	{ label: "projectTags.Weight", sortable: true, key: "weight_project_tag", valueKey: "weight_project_tag", type: "number" },
	{ label: "projectTags.ProjetsCount", sortable: true, key: "ProjectsProjectTags.Count", valueKey: "project_tags_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_project_tag",
	path: "/project-tags/",
	saveState: true,
	stateKey: "projetTagsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('projectTags.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/project-tags/new'">
				{{ $t('projectTags.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetTagsStore.projectTags" @ready="filterReady = true" :save-state="true" state-key="projetTagsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projetTagsStore.projectTags]"
		:filters="filter"
		:loading="projetTagsStore.projetTagsLoading"
		:total-count="Number(projetTagsStore.projetTagsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => projetTagsStore.getProjetTagByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
