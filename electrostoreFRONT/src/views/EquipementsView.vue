<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";

import { useViewScroll } from "@/composables";
import { EquipementStatus } from "@/enums";
import { useEquipementsStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauMeta } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const { t } = useI18n();
const equipementsStore = useEquipementsStore();

const equipementStatusOptions = {
	[EquipementStatus.Operational]: t("equipements.StatusOperational"),
	[EquipementStatus.InMaintenance]: t("equipements.StatusInMaintenance"),
	[EquipementStatus.OutOfService]: t("equipements.StatusOutOfService"),
	[EquipementStatus.Retired]: t("equipements.StatusRetired"),
};

const filter = ref<FilterLabel[]>([
	{ key: "reference_name_equipement", value: "", type: "text", label: "equipements.FilterName", compareMethod: "=like=" },
	{ key: "friendly_name_equipement", value: "", type: "text", label: "equipements.FilterFriendlyName", compareMethod: "=like=" },
	{ key: "status_equipement", value: undefined, type: "datalist", options: equipementStatusOptions, label: "equipements.FilterStatus", compareMethod: "==" },
]);
const tableauLabel = ref<TableauLabel[]>([
	{ label: "equipements.Name", sortable: true, key: "reference_name_equipement", valueKey: "reference_name_equipement", type: "text" },
	{ label: "equipements.FriendlyName", sortable: true, key: "friendly_name_equipement", valueKey: "friendly_name_equipement", type: "text" },
	{ label: "equipements.Description", sortable: false, key: "description_equipement", valueKey: "description_equipement", type: "text" },
	{ label: "equipements.Status", sortable: true, key: "status_equipement", valueKey: "status_equipement", type: "enum", options: equipementStatusOptions },
	{ label: "equipements.Img", sortable: false, key: "id_equipement", sourceKey: "id_equipement", type: "image",
		storeRessourceId: 1 },
]);
const tableauMeta = ref<TableauMeta>({
	key: "id_equipement",
	path: "/equipements/",
	saveState: true,
	stateKey: "equipementsTableState",
});
const filterReady = ref(false);
useViewScroll(false);
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('equipements.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/equipements/new'">
				{{ $t('equipements.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="equipementsStore.equipements" @ready="filterReady = true" :save-state="true" state-key="equipementsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[equipementsStore.equipements,equipementsStore.thumbnailsURL]"
		:filters="filter"
		:loading="equipementsStore.equipementsLoading"
		:total-count="Number(equipementsStore.equipementsTotalCount) || 0"
		:fetch-function="(limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => equipementsStore.getEquipementByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
