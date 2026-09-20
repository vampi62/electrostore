<script setup>
import { ref } from "vue";

import { useZonesStore } from "@/stores";
const zonesStore = useZonesStore();

const filter = ref([
	{ key: "name_zone", value: "", type: "text", label: "zones.FilterName", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "zones.Name", sortable: true, key: "name_zone", valueKey: "name_zone", type: "text" },
	{ label: "zones.Description", sortable: false, key: "description_zone", valueKey: "description_zone", type: "text" },
	{ label: "zones.XLength", sortable: true, key: "xlength_zone", valueKey: "xlength_zone", type: "number" },
	{ label: "zones.YLength", sortable: true, key: "ylength_zone", valueKey: "ylength_zone", type: "number" },
	{ label: "zones.Img", sortable: false, key: "id_zone", sourceKey: "id_zone", type: "image", fieldUrl: "url_thumbnail_zone",
		storeRessourceId: 1 },
	{ label: "zones.StoresCount", sortable: false, key: "stores_count", valueKey: "stores_count", type: "number" },
]);
const tableauMeta = ref({
	key: "id_zone",
	path: "/zones/",
	saveState: true,
	stateKey: "zonesTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('zones.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/zones/new'">
				{{ $t('zones.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="zonesStore.zones" @ready="filterReady = true" :save-state="true" state-key="zonesFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[zonesStore.zones,zonesStore.thumbnailsURL]"
		:filters="filter"
		:loading="zonesStore.zonesLoading"
		:total-count="Number(zonesStore.zonesTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => zonesStore.getZoneByInterval(limit, offset, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
