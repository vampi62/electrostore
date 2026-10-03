<script setup lang="ts">
import { useViewScroll } from "@/composables";
import { ref } from "vue";

import { useProjectTagsStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauMeta } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
const projectTagsStore = useProjectTagsStore();

const filter = ref<FilterLabel[]>([
	{ key: "name_project_tag", value: "", type: "text", label: "projectTags.FilterName", compareMethod: "=like=" },
	{ key: "weight_project_tag", value: "", type: "number", label: "projectTags.FilterWeightMin", compareMethod: "=ge=" },
	{ key: "weight_project_tag", value: "", type: "number", label: "projectTags.FilterWeightMax", compareMethod: "=le=" },
]);
const tableauLabel = ref<TableauLabel[]>([
	{ label: "projectTags.Name", sortable: true, key: "name_project_tag", valueKey: "name_project_tag", type: "text" },
	{ label: "projectTags.Weight", sortable: true, key: "weight_project_tag", valueKey: "weight_project_tag", type: "number" },
	{ label: "projectTags.ProjectsCount", sortable: true, key: "ProjectsProjectTags.Count", valueKey: "project_tags_count", type: "number" },
]);
const tableauMeta = ref<TableauMeta>({
	key: "id_project_tag",
	path: "/project-tags/",
	saveState: true,
	stateKey: "projectTagsTableState",
});
const filterReady = ref(false);
useViewScroll(false);
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
		<FilterContainer :filters="filter" :store-data="projectTagsStore.projectTags" @ready="filterReady = true" :save-state="true" state-key="projectTagsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projectTagsStore.projectTags]"
		:filters="filter"
		:loading="projectTagsStore.projectTagsLoading"
		:total-count="Number(projectTagsStore.projectTagsTotalCount) || 0"
		:fetch-function="(limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => projectTagsStore.getProjectTagByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
