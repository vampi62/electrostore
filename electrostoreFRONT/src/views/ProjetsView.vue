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
	{ key: "status_projet", value: "", type: "datalist", typeData: "number", options: projetTypeStatus, label: "projets.FilterStatus", compareMethod: "==" },
	{ key: "nom_projet", value: "", type: "text", label: "projets.FilterNom", compareMethod: "=like=" },
	{ key: "url_projet", value: "", type: "text", label: "projets.FilterUrl", compareMethod: "=like=" },
	{ key: "date_debut_projet", value: "", type: "date", label: "projets.FilterDate", compareMethod: "=ge=" },
	{ key: "date_fin_projet", value: "", type: "date", label: "projets.FilterDateEnd", compareMethod: "=ge=" },
	{ key: "ProjetsItems.Item.reference_name_item", value: "", type: "text", label: "projets.FilterItem", compareMethod: "=like=" },
	{ key: "ProjetsProjetTags.ProjetTag.nom_projet_tag", value: "", type: "text", label: "projets.FilterTag", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "projets.Name", sortable: true, key: "nom_projet", valueKey: "nom_projet", type: "text" },
	{ label: "projets.Description", sortable: false, key: "description_projet", valueKey: "description_projet", type: "text" },
	{ label: "projets.Url", sortable: true, key: "url_projet", valueKey: "url_projet", type: "text" },
	{ label: "projets.Status", sortable: true, key: "status_projet", valueKey: "status_projet", type: "enum", options: projetTypeStatus },

	{ label: "projets.Items", sortable: false, key: "", sourceKey: "id_projet", type: "link-list", 
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_projet", storeLinkKeyJoinRessource: "id_item", valueKey: "reference_name_item",
		ressourcePrint: [{ from: "link", valueKey: "qte_projet_item" }, { from: "text", text: " - " }, { from: "ressource", valueKey: "reference_name_item" }] },
		
	{ label: "projets.Tags", sortable: false, key: "", sourceKey: "id_projet", type: "link-list", 
		storeLinkId: 3, storeRessourceId: 4, storeLinkKeyJoinSource: "id_projet", storeLinkKeyJoinRessource: "id_projet_tag", valueKey: "nom_projet_tag",
		ressourcePrint: [{ from: "ressource", valueKey: "nom_projet_tag" }] },

	{ label: "projets.DateStart", sortable: true, key: "date_debut_projet", valueKey: "date_debut_projet", type: "date" },
	{ label: "projets.DateEnd", sortable: true, key: "date_fin_projet", valueKey: "date_fin_projet", type: "date" },
]);
const tableauMeta = ref({
	key: "id_projet",
	path: "/projets/",
	expand: ["projets_items", "projets_projet_tags"],
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
