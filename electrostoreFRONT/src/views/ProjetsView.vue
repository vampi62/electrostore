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
	filter.value[5].options = Object.fromEntries(Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]));
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
	filter.value[6].options = Object.fromEntries(Object.values(projetTagsStore.projetTags).map((tag) => [tag.id_projet_tag, tag.nom_projet_tag]));
}

const projetTypeStatus = ref({ [ProjetStatus.NotStarted]: t("projets.Status0"), [ProjetStatus.InProgress]: t("projets.Status1"),
	[ProjetStatus.Completed]: t("projets.Status2"), [ProjetStatus.OnHold]: t("projets.Status3"),
	[ProjetStatus.Cancelled]: t("projets.Status4"), [ProjetStatus.Archived]: t("projets.Status5") });

const filter = ref([
	{ key: "status_projet", value: "", type: "datalist", options: projetTypeStatus, label: "projets.FilterStatus", compareMethod: "=" },
	{ key: "nom_projet", value: "", type: "text", label: "projets.FilterNom", compareMethod: "contain" },
	{ key: "url_projet", value: "", type: "text", label: "projets.FilterUrl", compareMethod: "contain" },
	{ key: "date_debut_projet", value: "", type: "date", label: "projets.FilterDate", compareMethod: ">=" },
	{ key: "date_fin_projet", value: "", type: "date", label: "projets.FilterDateEnd", compareMethod: ">=" },
	{ key: "id_item", subPath: "projets_items", value: "", type: "datalist", typeData: "int", options: Object.fromEntries(Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item])), label: "projets.FilterItem", compareMethod: "=" },
	{ key: "id_projet_tag", subPath: "projets_projet_tags", value: "", type: "datalist", typeData: "int", options: Object.fromEntries(Object.values(projetTagsStore.projetTags).map((tag) => [tag.id_projet_tag, tag.nom_projet_tag])), label: "projets.FilterTag", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "projets.Name", sortable: true, key: "nom_projet", type: "text" },
	{ label: "projets.Description", sortable: false, key: "description_projet", type: "text" },
	{ label: "projets.Url", sortable: true, key: "url_projet", type: "text" },
	{ label: "projets.Status", sortable: true, key: "status_projet", type: "enum", options: projetTypeStatus },
	{ label: "projets.Items", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_projet", keyStoreLink: "id_item", ressourcePrint: [{ type: "link", key: "qte_projet_item" }, { type: "text", key: " - " }, { type: "ressource", key: "reference_name_item" }] } },
	{ label: "projets.Tags", sortable: false, key: "", type: "list", list: { idStoreLink: 3, idStoreRessource: 4, key: "id_projet", keyStoreLink: "id_projet_tag", ressourcePrint: [{ type: "ressource", key: "nom_projet_tag" }] } },
	{ label: "projets.DateStart", sortable: true, key: "date_debut_projet", type: "date" },
	{ label: "projets.DateEnd", sortable: true, key: "date_fin_projet", type: "date" },
]);
const tableauMeta = ref({
	key: "id_projet",
	path: "/projets/",
	expand: ["projets_items", "projets_projet_tags"],
});
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
		<FilterContainer :filters="filter" :store-data="projetsStore.projets" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[projetsStore.projets,projetsStore.items,itemsStore.items,projetsStore.projetTagProjet,projetTagsStore.projetTags]"
		:filters="filter"
		:loading="projetsStore.projetsLoading"
		:total-count="Number(projetsStore.projetsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => projetsStore.getProjetByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset), (minOffset, maxOffset) => fetchItemData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
