<script setup>
import { onMounted, ref } from "vue";

import { useAuthStore, useStoresStore, useTagsStore } from "@/stores";
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	let tagsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await storesStore.getStoreByInterval(limit, offset, ["stores_tags"]);
		offset += limit;
	} while (offset < storesStore.storesTotalCount);
	for (const store in storesStore.stores) {
		for (const tag in storesStore.storeTags[store]) {
			tagsLink.add(tag);
		}
	}
	let tagsNotFound = [];
	for (const tag of Array.from(tagsLink)) {
		if (!tagsStore.tags[tag]) {
			tagsNotFound.push(tag);
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
	filter.value[4].options = Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "nom_store", value: "", type: "text", label: "store.VStoresFilterName", compareMethod: "contain" },
	{ key: "mqtt_name_store", value: "", type: "text", label: "store.VStoresFilterMqttName", compareMethod: "contain" },
	{ key: "xlength_store", value: "", type: "number", label: "store.VStoresFilterXLength", compareMethod: "<=" },
	{ key: "ylength_store", value: "", type: "number", label: "store.VStoresFilterYLength", compareMethod: "<=" },
	{ key: "id_tag", subPath: "stores_tags", value: "", type: "select", typeData: "int", options: Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]), label: "store.VStoresFilterTag", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "store.VStoresName", sortable: true, key: "nom_store", type: "text" },
	{ label: "store.VStoresXLength", sortable: true, key: "xlength_store", type: "number" },
	{ label: "store.VStoresYLength", sortable: true, key: "ylength_store", type: "number" },
	{ label: "store.VStoresMqttName", sortable: true, key: "mqtt_name_store", type: "text" },
	{ label: "store.VStoresTagsList", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_store", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "nom_tag" }] } },
]);
const tableauMeta = ref({
	key: "id_store",
	path: "/stores/",
});
const filteredStores = ref([]);
const updateFilteredStores = (newValue) => {
	filteredStores.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('store.VStoresTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 2"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/stores/new'">
				Ajouter
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="storesStore.stores" @output-filter="updateFilteredStores" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredStores,storesStore.storeTags,tagsStore.tags]"
		:loading="storesStore.storesLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
