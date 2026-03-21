<script setup>
import { ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useCommandsStore, useItemsStore } from "@/stores";
const commandsStore = useCommandsStore();
const itemsStore = useItemsStore();

async function fetchItemData(minOffset, maxOffset) {
	let itemsNotFound = [];
	for (let id = minOffset; id < maxOffset; id++) {
		for (const item in commandsStore.items[id]) {
			if (!itemsStore.items[item]) {
				itemsNotFound.push(item);
			}
		}
	}
	if (itemsNotFound.length > 0) {
		await itemsStore.getItemByList(itemsNotFound);
	}
}

const filter = ref([
	{ key: "status_command", value: "", type: "datalist", options: { ["En attente"]: t("commands.FilterStatus1"), ["En cours"]: t("commands.FilterStatus2"), ["Terminée"]: t("commands.FilterStatus3"), ["Annulée"]: t("commands.FilterStatus4") }, label: "commands.FilterStatus", compareMethod: "==" },
	{ key: "date_command", value: "", type: "date", label: "commands.FilterDate", compareMethod: "=ge=" },
	{ key: "url_command", value: "", type: "text", label: "commands.FilterURL", compareMethod: "=like=" },
	{ key: "prix_command", value: "", type: "number", label: "commands.FilterPriceMin", compareMethod: "=ge=" },
	{ key: "prix_command", value: "", type: "number", label: "commands.FilterPriceMax", compareMethod: "=le=" },
	{ key: "date_livraison_command", value: "", type: "date", label: "commands.FilterDateL", compareMethod: "=ge=" },
	{ key: "CommandsItems.Item.reference_name_item", value: "", type: "text", label: "commands.FilterItem", compareMethod: "=like=" },
]);
const tableauLabel = ref([
	{ label: "commands.Status", sortable: true, key: "status_command", valueKey: "status_command", type: "text" },
	{ label: "commands.Date", sortable: true, key: "date_command", valueKey: "date_command", type: "date" },
	{ label: "commands.URL", sortable: true, key: "url_command", valueKey: "url_command", type: "text" },
	{ label: "commands.Prix", sortable: true, key: "prix_command", valueKey: "prix_command", type: "text" },

	{ label: "commands.ItemList", sortable: false, key: "commands.ItemList", sourceKey: "id_command", type: "link-list", 
		storeLinkId: 1, storeRessourceId: 2, storeLinkKeyJoinSource: "id_command", storeLinkKeyJoinRessource: "id_item", valueKey: "reference_name_item",
		ressourcePrint: [{ from: "link", valueKey: "qte_command_item" }, { from: "text", text: " - " }, { from: "ressource", valueKey: "reference_name_item" }] },

	{ label: "commands.DateL", sortable: true, key: "date_livraison_command", valueKey: "date_livraison_command", type: "date" },
]);
const tableauMeta = ref({
	key: "id_command",
	path: "/commands/",
	expand: ["commands_items"],
	saveState: true,
	stateKey: "commandsTableState",
});
const filterReady = ref(false);
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('commands.Title') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded cursor-pointer inline-block mb-2">
			<RouterLink :to="'/commands/new'">
				{{ $t('commands.Add') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="commandsStore.commands" @ready="filterReady = true" :save-state="true" state-key="commandsFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[commandsStore.commands,commandsStore.items,itemsStore.items]"
		:filters="filter"
		:loading="commandsStore.commandsLoading"
		:total-count="Number(commandsStore.commandsTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => commandsStore.getCommandByInterval(limit, offset, expand, filter, sort, clear)"
		:list-fetch-function="[(minOffset, maxOffset) => fetchItemData(minOffset, maxOffset)]"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>