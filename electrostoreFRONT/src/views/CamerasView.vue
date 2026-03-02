<script setup>
import { ref } from "vue";

import { useAuthStore, useCamerasStore } from "@/stores";
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

const filter = ref([
	{ key: "nom_camera", value: "", type: "text", label: "cameras.FilterName", compareMethod: "=like=" },
	{ key: "url_camera", value: "", type: "text", label: "cameras.FilterUrl", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "cameras.Name", sortable: true, key: "nom_camera", sourceKey: "nom_camera", type: "text" },
	{ label: "cameras.Url", sortable: true, key: "url_camera", sourceKey: "url_camera", type: "text" },
	{ label: "cameras.User", sortable: false, key: "", sourceKey: "", type: "bool", condition: "rowData?.user_camera == '' && rowData?.mdp_camera == ''" },
	{ label: "cameras.Network", sortable: false, key: "", sourceKey: "", type: "bool", condition: "store[1]?.[rowData.id_camera]?.network" },
	{ label: "cameras.Status", sortable: false, key: "", sourceKey: "", type: "bool", condition: "store[1]?.[rowData.id_camera]?.statusCode == 200" },
]);
const tableauMeta = ref({
	key: "id_camera",
	path: "/cameras/",
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('cameras.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2]),
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/cameras/new'">
				{{ $t('cameras.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('cameras.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="camerasStore.cameras" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[camerasStore.cameras,camerasStore.status]"
		:filters="filter"
		:loading="camerasStore.loading"
		:total-count="Number(camerasStore.TotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => camerasStore.getCameraByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
