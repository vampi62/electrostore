<script setup>
import { ref } from "vue";

import { useStoresStore, useTagsStore, useAuthStore } from "@/stores";
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const authStore = useAuthStore();

async function fetchTagData(minOffset, maxOffset) {
	let tagsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const tag in storesStore.storeTags[id]) {
			if (!tagsStore.tags[tag]) {
				tagsNotFound.push(tag);
			}
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
	filter.value[6].options = Object.fromEntries(Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]));
}

const filter = ref([
	{ key: "nom_store", value: "", type: "text", label: "stores.FilterName", compareMethod: "contain" },
	{ key: "mqtt_name_store", value: "", type: "text", label: "stores.FilterMqttName", compareMethod: "contain" },
	{ key: "xlength_store", value: "", type: "number", label: "stores.FilterXLength", compareMethod: "<=" },
	{ key: "ylength_store", value: "", type: "number", label: "stores.FilterYLength", compareMethod: "<=" },
	{ key: "id_tag", subPath: "stores_tags", value: "", type: "datalist", typeData: "int", options: Object.fromEntries(Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag])), label: "stores.FilterTag", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "stores.Name", sortable: true, key: "nom_store", type: "text" },
	{ label: "stores.XLength", sortable: true, key: "xlength_store", type: "number" },
	{ label: "stores.YLength", sortable: true, key: "ylength_store", type: "number" },
	{ label: "stores.MqttName", sortable: true, key: "mqtt_name_store", type: "text" },
	{ label: "stores.TagsList", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_store", keyStoreLink: "id_tag", ressourcePrint: [{ type: "ressource", key: "nom_tag" }] } },
]);
const tableauMeta = ref({
	key: "id_store",
	path: "/stores/",
	expand: ["stores_tags"],
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('stores.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/stores/new'">
				{{ $t('stores.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('stores.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="storesStore.stores" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[storesStore.stores,storesStore.storeTags,tagsStore.tags]"
		:filters="filter"
		:loading="storesStore.storesLoading"
		:total-count="Number(storesStore.storesTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => storesStore.getStoreByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchTagData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
