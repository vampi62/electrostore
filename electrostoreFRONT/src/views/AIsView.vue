<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAisStore, useAuthStore } from "@/stores";
const IAStore = useAisStore();
const authStore = useAuthStore();

const filter = ref([
	{ key: "trained_ia", value: undefined, type: "datalist", typeData: "bool", options: { ["false"]: t("ais.FilterTrained1"), ["true"]: t("ais.FilterTrained2") }, label: "ais.FilterTrained", compareMethod: "==" },
	{ key: "updated_at", value: "", type: "date", label: "ais.FilterDate", compareMethod: "=ge=" },
	{ key: "name_ia", value: "", type: "text", label: "ais.FilterNom", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "ais.Name", sortable: true, key: "name_ia", valueKey: "name_ia", type: "text" },
	{ label: "ais.Description", sortable: false, key: "description_ia", valueKey: "description_ia", type: "text" },
	{ label: "ais.Date", sortable: true, key: "updated_at", valueKey: "updated_at", type: "date" },
	{ label: "ais.Trained", sortable: true, key: "trained_ia", valueKey: "trained_ia", type: "enum", options: { [false]: t("ais.FilterTrained1"), [true]: t("ais.FilterTrained2") } },
]);
const tableauMeta = ref({
	key: "id_ia",
	path: "/ai/",
	saveState: true,
	stateKey: "iasTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ais.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/ai/new'">
				{{ $t('ais.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('ais.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="IAStore.ais" @ready="filterReady = true" :save-state="true" state-key="iasFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[IAStore.ais]"
		:filters="filter"
		:loading="IAStore.loading"
		:total-count="Number(IAStore.TotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => IAStore.getIaByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
