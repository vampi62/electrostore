<script setup>
import { onMounted, ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useProjetsStore, useItemsStore } from "@/stores";
const projetsStore = useProjetsStore();
const itemsStore = useItemsStore();

async function fetchAllData() {
	let itemsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await projetsStore.getProjetByInterval(limit, offset, ["projets_items"]);
		offset += limit;
	} while (offset < projetsStore.projetsTotalCount);
	for (const projet in projetsStore.projets) {
		for (const item in projetsStore.items[projet]) {
			itemsLink.add(item);
		}
	}
	let itemsNotFound = [];
	for (const item of Array.from(itemsLink)) {
		if (!itemsStore.items[item]) {
			itemsNotFound.push(item);
		}
	}
	if (itemsNotFound.length > 0) {
		await itemsStore.getItemByList(itemsNotFound);
	}
	filter.value[5].options = Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "status_projet", value: "", type: "select", options: [["en attente", t("projet.VProjetsFilterStatus1")], ["en cours", t("projet.VProjetsFilterStatus2")], ["terminée", t("projet.VProjetsFilterStatus3")]], label: "projet.VProjetsFilterStatus", compareMethod: "=" },
	{ key: "date_debut_projet", value: "", type: "date", label: "projet.VprojetFilterDate", compareMethod: ">=" },
	{ key: "nom_projet", value: "", type: "text", label: "projet.VprojetFilterNom", compareMethod: "contain" },
	{ key: "url_projet", value: "", type: "text", label: "projet.VprojetFilterUrl", compareMethod: "contain" },
	{ key: "date_fin_projet", value: "", type: "date", label: "projet.VprojetFilterDateEnd", compareMethod: ">=" },
	{ key: "id_item", subPath: "projets_items", value: "", type: "select", typeData: "int", options: Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]), label: "projet.VprojetFilterItem", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "projet.VProjetsName", sortable: true, key: "nom_projet", type: "text" },
	{ label: "projet.VProjetsDescription", sortable: false, key: "description_projet", type: "text" },
	{ label: "projet.VProjetsUrl", sortable: true, key: "url_projet", type: "text" },
	{ label: "projet.VProjetsStatus", sortable: true, key: "status_projet", type: "text" },
	{ label: "projet.VProjetsDateStart", sortable: true, key: "date_debut_projet", type: "date" },
	{ label: "projet.VProjetsItems", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_projet", keyStoreLink: "id_item", ressourcePrint: [{ type: "link", key: "qte_projet_item" }, { type: "text", key: " - " }, { type: "ressource", key: "reference_name_item" }] } },
	{ label: "projet.VProjetsDateEnd", sortable: true, key: "date_fin_projet", type: "date" },
]);
const tableauMeta = ref({
	key: "id_projet",
	path: "/projets/",
});
const filteredProjets = ref([]);
const updateFilteredProjets = (newValue) => {
	filteredProjets.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ t('projet.VProjetsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projets/new'">
				{{ t('projet.VProjetsAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetsStore.projets" @output-filter="updateFilteredProjets" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredProjets,projetsStore.items,itemsStore.items]"
		:loading="projetsStore.projetsLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
