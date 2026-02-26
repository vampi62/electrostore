<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useIasStore, useAuthStore } from "@/stores";
const IAStore = useIasStore();
const authStore = useAuthStore();

const filter = ref([
	{ key: "trained_ia", value: undefined, type: "datalist", typeData: "bool", options: [["false", t("ias.FilterTrained1")], ["true", t("ias.FilterTrained2")]], label: "ias.FilterTrained", compareMethod: "=" },
	{ key: "updated_at", value: "", type: "date", label: "ias.FilterDate", compareMethod: ">=" },
	{ key: "nom_ia", value: "", type: "text", label: "ias.FilterNom", compareMethod: "contain" },
]);
const tableauLabel = ref([
	{ label: "ias.Name", sortable: true, key: "nom_ia", type: "text" },
	{ label: "ias.Description", sortable: false, key: "description_ia", type: "text" },
	{ label: "ias.Date", sortable: true, key: "updated_at", type: "date" },
	{ label: "ias.Trained", sortable: true, key: "trained_ia", type: "bool", condition: "rowData.trained_ia" },
]);
const tableauMeta = ref({
	key: "id_ia",
	path: "/ia/",
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ias.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/ia/new'">
				{{ $t('ias.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('ias.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="IAStore.ias" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[IAStore.ias]"
		:filters="filter"
		:loading="IAStore.loading"
		:total-count="Number(IAStore.TotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => IAStore.getIaByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
