<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useProjetsStore, useItemsStore, useProjetTagsStore } from "@/stores";
const projetsStore = useProjetsStore();
const itemsStore = useItemsStore();
const projetTagsStore = useProjetTagsStore();

import { ProjetStatus } from "@/enums";

async function fetchItemData(minOffset, maxOffset) {
	let itemsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const item in projetsStore.items[id]) {
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
		for (const tag in projetsStore.projetTagProjet[id]) {
			if (!projetTagsStore.projetTags[tag]) {
				tagsNotFound.push(tag);
			}
		}
	}
	if (tagsNotFound.length > 0) {
		await projetTagsStore.getProjetTagByList(tagsNotFound);
	}
}

const projetTypeStatus = ref({ [ProjetStatus.NotStarted]: t("projets.Status0"), [ProjetStatus.InProgress]: t("projets.Status1"),
	[ProjetStatus.Completed]: t("projets.Status2"), [ProjetStatus.OnHold]: t("projets.Status3"),
	[ProjetStatus.Cancelled]: t("projets.Status4"), [ProjetStatus.Archived]: t("projets.Status5") });

const filter = ref([
	{ key: "status_project", value: "", type: "datalist", typeData: "number", options: projetTypeStatus, label: "projets.FilterStatus", compareMethod: "==" },
	{ key: "name_project", value: "", type: "text", label: "projets.FilterNom", compareMethod: "=like=" },
	{ key: "url_project", value: "", type: "text", label: "projets.FilterUrl", compareMethod: "=like=" },
	{ key: "date_start_project", value: "", type: "date", label: "projets.FilterDate", compareMethod: "=ge=" },
	{ key: "date_end_project", value: "", type: "date", label: "projets.FilterDateEnd", compareMethod: "=ge=" },
	{ key: "ProjetsItems.Item.reference_name_item", value: "", type: "text", label: "projets.FilterItem", compareMethod: "=like=" },
	{ key: "ProjetsProjetTags.ProjetTag.name_project_tag", value: "", type: "text", label: "projets.FilterTag", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "projets.Name", sortable: true, key: "name_project", valueKey: "name_project", type: "text" },
	{ label: "projets.Description", sortable: false, key: "description_project", valueKey: "description_project", type: "text" },
	{ label: "projets.Url", sortable: true, key: "url_project", valueKey: "url_project", type: "text" },
	{ label: "projets.Status", sortable: true, key: "status_project", valueKey: "status_project", type: "enum", options: projetTypeStatus },

	{ label: "projets.Items", sortable: false, key: "", sourceKey: "id_project", type: "link-list", 
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_project", storeLinkKeyJoinRessource: "id_item", valueKey: "reference_name_item",
		ressourcePrint: [{ from: "link", valueKey: "quantity_project_item" }, { from: "text", text: " - " }, { from: "ressource", valueKey: "reference_name_item" }] },
		
	{ label: "projets.Tags", sortable: false, key: "", sourceKey: "id_project", type: "link-list", 
		storeLinkId: 3, storeRessourceId: 4, storeLinkKeyJoinSource: "id_project", storeLinkKeyJoinRessource: "id_project_tag", valueKey: "name_project_tag",
		ressourcePrint: [{ from: "ressource", valueKey: "name_project_tag" }] },

	{ label: "projets.DateStart", sortable: true, key: "date_start_project", valueKey: "date_start_project", type: "date" },
	{ label: "projets.DateEnd", sortable: true, key: "date_end_project", valueKey: "date_end_project", type: "date" },
]);
const tableauMeta = ref({
	key: "id_project",
	path: "/projets/",
	expand: ["project_items", "project_tags"],
	saveState: true,
	stateKey: "projetsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ t('projets.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projets/new'">
				{{ t('projets.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetsStore.projets" @ready="filterReady = true" :save-state="true" state-key="projetsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projetsStore.projets,projetsStore.items,itemsStore.items,projetsStore.projetTagProjet,projetTagsStore.projetTags]"
		:filters="filter"
		:loading="projetsStore.projetsLoading"
		:total-count="Number(projetsStore.projetsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => projetsStore.getProjetByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset), (minOffset, maxOffset) => fetchItemData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
