<script setup>
import { onMounted, ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useIasStore } from "@/stores";
const IAStore = useIasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	let offset = 0;
	const limit = 100;
	do {
		await IAStore.getIaByInterval(limit, offset);
		offset += limit;
	} while (offset < IAStore.TotalCount);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "trained_ia", value: undefined, type: "datalist", dataType: "bool", options: [["false", t("ia.VIasFilterTrained1")], ["true", t("ia.VIasFilterTrained2")]], label: "ia.VIasFilterTrained", compareMethod: "=" },
	{ key: "updated_at", value: "", type: "date", label: "ia.VIasFilterDate", compareMethod: ">=" },
	{ key: "nom_ia", value: "", type: "text", label: "ia.VIasFilterNom", compareMethod: "contain" },
]);
const tableauLabel = ref([
	{ label: "ia.VIasName", sortable: true, key: "nom_ia", type: "text" },
	{ label: "ia.VIasDescription", sortable: false, key: "description_ia", type: "text" },
	{ label: "ia.VIasDate", sortable: true, key: "updated_at", type: "date" },
	{ label: "ia.VIasTrained", sortable: true, key: "trained_ia", type: "bool", condition: "rowData.trained_ia" },
]);
const tableauMeta = ref({
	key: "id_ia",
	path: "/ia/",
});
const filteredIas = ref([]);
const updateFilteredIas = (newValue) => {
	filteredIas.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ia.VIasTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 2"
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/ia/new'">
				{{ $t('ia.VIasAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="IAStore.ias" @output-filter="updateFilteredIas" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredIas]"
		:loading="IAStore.loading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
