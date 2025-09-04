<script setup>
import { onMounted, ref } from "vue";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useCommandsStore, useItemsStore } from "@/stores";
const commandsStore = useCommandsStore();
const itemsStore = useItemsStore();

async function fetchAllData() {
	let itemsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await commandsStore.getCommandByInterval(limit, offset, ["commands_items"]);
		offset += limit;
	} while (offset < commandsStore.commandsTotalCount);
	for (const command in commandsStore.commands) {
		for (const item in commandsStore.items[command]) {
			itemsLink.add(item);
		}
	}
	let itemsNotFound = [];
	for (const item of Array.from(itemsLink)) {
		if (!itemsStore.items[item]) {
			itemsNotFound.push(item);
		}
	}
	if (itemsNotFound.length > 0) {
		await itemsStore.getItemByList(itemsNotFound);
	}
	filter.value[6].options = Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "status_command", value: "", type: "datalist", options: [["En attente", t("command.VCommandsFilterStatus1")], ["En cours", t("command.VCommandsFilterStatus2")], ["Terminée", t("command.VCommandsFilterStatus3")], ["Annulée", t("command.VCommandsFilterStatus4")]], label: "command.VCommandsFilterStatus", compareMethod: "=" },
	{ key: "date_command", value: "", type: "date", label: "command.VCommandsFilterDate", compareMethod: ">=" },
	{ key: "url_command", value: "", type: "text", label: "command.VCommandsFilterURL", compareMethod: "contain" },
	{ key: "prix_command", value: "", type: "number", label: "command.VCommandsFilterPriceMin", compareMethod: ">=" },
	{ key: "prix_command", value: "", type: "number", label: "command.VCommandsFilterPriceMax", compareMethod: "<=" },
	{ key: "date_livraison_command", value: "", type: "date", label: "command.VCommandsFilterDateL", compareMethod: ">=" },
	{ key: "id_item", subPath: "commands_items", value: "", type: "datalist", typeData: "int", options: Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]), label: "command.VCommandsFilterItem", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "command.VCommandsStatus", sortable: true, key: "status_command", type: "text" },
	{ label: "command.VCommandsDate", sortable: true, key: "date_command", type: "date" },
	{ label: "command.VCommandsURL", sortable: true, key: "url_command", type: "text" },
	{ label: "command.VCommandsPrix", sortable: true, key: "prix_command", type: "text" },
	{ label: "command.VCommandsItemList", sortable: false, key: "", type: "list", list: { idStoreLink: 1, idStoreRessource: 2, key: "id_command", keyStoreLink: "id_item", ressourcePrint: [{ type: "link", key: "qte_command_item" }, { type: "text", key: " - " }, { type: "ressource", key: "reference_name_item" }] } },
	{ label: "command.VCommandsDateL", sortable: true, key: "date_livraison_command", type: "date" },
]);
const tableauMeta = ref({
	key: "id_command",
	path: "/commands/",
});
const filteredCommands = ref([]);
const updateFilteredCommands = (newValue) => {
	filteredCommands.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('command.VCommandsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/commands/new'">
				{{ $t('command.VCommandsAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="commandsStore.commands" @output-filter="updateFilteredCommands" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredCommands,commandsStore.items,itemsStore.items]"
		:loading="commandsStore.commandsLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>