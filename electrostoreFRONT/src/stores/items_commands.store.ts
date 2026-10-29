import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useCommandsStore } from "@/stores";

import type { Ready, Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadCommandItemDto = components["schemas"]["ReadCommandItemDto"];
type ReadExtendedCommandItemDto = components["schemas"]["ReadExtendedCommandItemDto"];
type CreateCommandItemByItemDto = components["schemas"]["CreateCommandItemByItemDto"];
type UpdateCommandItemDto = components["schemas"]["UpdateCommandItemDto"];

const itemCommandResource = createResource<{ readExtended: ReadExtendedCommandItemDto, readBasic: ReadCommandItemDto, create: CreateCommandItemByItemDto, update: UpdateCommandItemDto, createBulk: CreateCommandItemByItemDto[] }>({
	compositeKey: ["id_item", "id_command"],
	path: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/command` }],
	entityPath: [{ keys: ["id_item", "id_command"], url: ({ id_item, id_command }) => `/item/${id_item}/command/${id_command}` }],
	stateKey: "itemCommands",
	countKey: "itemCommandsTotalCount",
	loadingKey: "itemCommandsLoading",
	editionKey: "itemCommandEdition",
	readyKey: "itemCommandReady",
	onHydrate: (store, entity: ReadExtendedCommandItemDto, expand) => {
		if (expand.includes("command") && entity.command && entity.id_command) {
			const commandsStore = useCommandsStore();
			commandsStore.commands[entity.id_command] = entity.command;
		}
	},
});

// Records are indexed by the composite key "<id_item>:<id_command>"
export const useItemsCommandsStore = defineStore("itemsCommands", {
	state: () => ({
		itemCommandsLoading: false,
		// indexed by scope: "id_item=3"
		itemCommandsTotalCount: {} as Record<string, number>,
		itemCommands: {} as Record<string, ReadCommandItemDto>,
		itemCommandEdition: {} as Record<string, Edition<{ create: CreateCommandItemByItemDto, update: UpdateCommandItemDto }>>,
		itemCommandReady: {} as Record<string, Ready<{ create: CreateCommandItemByItemDto, update: UpdateCommandItemDto }>>,
	}),
	getters: {
		// the records of an item, indexed by id_command
		itemCommandsByItem: (state) => (idItem: string) => itemCommandResource.slice(state.itemCommands, { id_item: idItem }),
		itemCommandEditionByItem: (state) => (idItem: string) => itemCommandResource.slice(state.itemCommandEdition, { id_item: idItem }),
		itemCommandReadyByItem: (state) => (idItem: string) => itemCommandResource.slice(state.itemCommandReady, { id_item: idItem }),
		itemCommandsCountByItem: (state) => (idItem: string) => state.itemCommandsTotalCount[itemCommandResource.toScopeKey({ id_item: idItem })] ?? 0,
	},
	actions: {
		getItemCommandByInterval: itemCommandResource.getByInterval,
		getItemCommandById: itemCommandResource.getById,
		createItemCommand: itemCommandResource.create,
		updateItemCommand: itemCommandResource.update,
		deleteItemCommand: itemCommandResource.remove,
		createItemCommandBulk: itemCommandResource.createBulk,
		hydrateItemCommands: itemCommandResource.hydrate,
		setItemCommandEdition: itemCommandResource.setEdition,
		removeItemCommandEdition: itemCommandResource.removeEdition,
		removeItemCommandReady: itemCommandResource.removeReady,
		setItemCommandStagedSlice: itemCommandResource.setStagedSlice,
		valideItemCommandEditionByKey: itemCommandResource.valideEditionByKey,
		copyItemCommandByKeyPart: itemCommandResource.copyByKeyPart,
		clearItemCommandEdition: itemCommandResource.clearEdition,
		pushItemCommandChange: itemCommandResource.pushChange,
	},
});
