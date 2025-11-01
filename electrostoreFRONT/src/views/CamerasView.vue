<script setup>
import { onMounted, ref } from "vue";

import { useAuthStore, useCamerasStore } from "@/stores";
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	let offset = 0;
	const limit = 100;
	do {
		await camerasStore.getCameraByInterval(limit, offset);
		offset += limit;
	} while (offset < camerasStore.TotalCount);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "nom_camera", value: "", type: "text", label: "camera.VCamerasFilterName", compareMethod: "contain" },
	{ key: "url_camera", value: "", type: "text", label: "camera.VCamerasFilterUrl", compareMethod: "contain" },
]);
const tableauLabel = ref([
	{ label: "camera.VCamerasName", sortable: true, key: "nom_camera", type: "text" },
	{ label: "camera.VCamerasUrl", sortable: true, key: "url_camera", type: "text" },
	{ label: "camera.VCamerasUser", sortable: false, key: "", type: "bool", condition: "rowData?.user_camera == '' && rowData?.mdp_camera == ''" },
	{ label: "camera.VCamerasNetwork", sortable: false, key: "", type: "bool", condition: "store[1]?.[rowData.id_camera]?.network" },
	{ label: "camera.VCamerasStatus", sortable: false, key: "", type: "bool", condition: "store[1]?.[rowData.id_camera]?.statusCode == 200" },
]);
const tableauMeta = ref({
	key: "id_camera",
	path: "/cameras/",
});
// recupere les donne filtrer du component FilterContainer.filteredData
const filteredCameras = ref([]);
const updateFilteredCameras = (newValue) => {
	filteredCameras.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('camera.VCamerasTitle') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.user?.role_user === 2,
				'bg-gray-400 cursor-not-allowed': authStore.user?.role_user !== 2
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.user?.role_user === 2" :to="'/cameras/new'">
				{{ $t('camera.VCamerasAdd') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('camera.VCamerasAdd') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="camerasStore.cameras" @output-filter="updateFilteredCameras" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredCameras,camerasStore.status]"
		:loading="camerasStore.loading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
