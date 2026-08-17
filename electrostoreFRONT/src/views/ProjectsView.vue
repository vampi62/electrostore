<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useProjectsStore, useItemsStore, useProjectTagsStore } from "@/stores";
const projectsStore = useProjectsStore();
const itemsStore = useItemsStore();
const projectTagsStore = useProjectTagsStore();

import { ProjectStatus } from "@/enums";

async function fetchItemData(minOffset, maxOffset) {
	let itemsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const item in projectsStore.items[id]) {
			if (!itemsStore.items[item]) {
				itemsNotFound.push(item);
			}
		}
	}
	if (itemsNotFound.length > 0) {
		await itemsStore.getItemByList(itemsNotFound);
	}
}
async function fetchTagData(minOffset, maxOffset) {
	let tagsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const tag in projectsStore.projectTagProject[id]) {
			if (!projectTagsStore.projectTags[tag]) {
				tagsNotFound.push(tag);
			}
		}
	}
	if (tagsNotFound.length > 0) {
		await projectTagsStore.getProjectTagByList(tagsNotFound);
	}
}

const projectTypeStatus = ref({ [ProjectStatus.NotStarted]: t("projects.Status0"), [ProjectStatus.InProgress]: t("projects.Status1"),
	[ProjectStatus.Completed]: t("projects.Status2"), [ProjectStatus.OnHold]: t("projects.Status3"),
	[ProjectStatus.Cancelled]: t("projects.Status4"), [ProjectStatus.Archived]: t("projects.Status5") });

const filter = ref([
	{ key: "status_project", value: "", type: "datalist", typeData: "number", options: projectTypeStatus, label: "projects.FilterStatus", compareMethod: "==" },
	{ key: "name_project", value: "", type: "text", label: "projects.FilterNom", compareMethod: "=like=" },
	{ key: "url_project", value: "", type: "text", label: "projects.FilterUrl", compareMethod: "=like=" },
	{ key: "date_start_project", value: "", type: "date", label: "projects.FilterDate", compareMethod: "=ge=" },
	{ key: "date_end_project", value: "", type: "date", label: "projects.FilterDateEnd", compareMethod: "=ge=" },
	{ key: "ProjectsItems.Item.reference_name_item", value: "", type: "text", label: "projects.FilterItem", compareMethod: "=like=" },
	{ key: "ProjectsProjectTags.ProjectTag.name_project_tag", value: "", type: "text", label: "projects.FilterTag", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "projects.Name", sortable: true, key: "name_project", valueKey: "name_project", type: "text" },
	{ label: "projects.Description", sortable: false, key: "description_project", valueKey: "description_project", type: "text" },
	{ label: "projects.Url", sortable: true, key: "url_project", valueKey: "url_project", type: "text" },
	{ label: "projects.Status", sortable: true, key: "status_project", valueKey: "status_project", type: "enum", options: projectTypeStatus },

	{ label: "projects.Items", sortable: false, key: "", sourceKey: "id_project", type: "link-list", 
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_project", storeLinkKeyJoinRessource: "id_item", valueKey: "reference_name_item",
		ressourcePrint: [{ from: "link", valueKey: "quantity_project_item" }, { from: "text", text: " - " }, { from: "ressource", valueKey: "reference_name_item" }] },
		
	{ label: "projects.Tags", sortable: false, key: "", sourceKey: "id_project", type: "link-list", 
		storeLinkId: 3, storeRessourceId: 4, storeLinkKeyJoinSource: "id_project", storeLinkKeyJoinRessource: "id_project_tag", valueKey: "name_project_tag",
		ressourcePrint: [{ from: "ressource", valueKey: "name_project_tag" }] },

	{ label: "projects.DateStart", sortable: true, key: "date_start_project", valueKey: "date_start_project", type: "date" },
	{ label: "projects.DateEnd", sortable: true, key: "date_end_project", valueKey: "date_end_project", type: "date" },
]);
const tableauMeta = ref({
	key: "id_project",
	path: "/projects/",
	expand: ["project_items", "project_tags"],
	saveState: true,
	stateKey: "projectsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ t('projects.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projects/new'">
				{{ t('projects.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projectsStore.projects" @ready="filterReady = true" :save-state="true" state-key="projectsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projectsStore.projects,projectsStore.items,itemsStore.items,projectsStore.projectTagProject,projectTagsStore.projectTags]"
		:filters="filter"
		:loading="projectsStore.projectsLoading"
		:total-count="Number(projectsStore.projectsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => projectsStore.getProjectByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset), (minOffset, maxOffset) => fetchItemData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
