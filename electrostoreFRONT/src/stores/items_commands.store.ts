import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useCommandsStore, useItemsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadCommandItemDto = components["schemas"]["ReadCommandItemDto"];
type ReadExtendedCommandItemDto = components["schemas"]["ReadExtendedCommandItemDto"];
type CreateCommandItemByItemDto = components["schemas"]["CreateCommandItemByItemDto"];
type CreateCommandItemByCommandDto = components["schemas"]["CreateCommandItemByCommandDto"];
type UpdateCommandItemDto = components["schemas"]["UpdateCommandItemDto"];
type CreateItemCommand = CreateCommandItemByItemDto | CreateCommandItemByCommandDto;

const itemCommandResource = createResource<{ readExtended: ReadExtendedCommandItemDto, readBasic: ReadCommandItemDto, create: CreateItemCommand, update: UpdateCommandItemDto, createBulk: CreateItemCommand[] }>({
	compositeKey: ["id_item", "id_command"],
	path: [
		{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/command` },
		{ keys: ["id_command"], url: ({ id_command }) => `/command/${id_command}/item` },
	],
	entityPath: [
		{ keys: ["id_item", "id_command"], url: ({ id_item, id_command }) => `/item/${id_item}/command/${id_command}` },
	],
	stateKey: "itemCommands",
	countKey: "itemCommandsTotalCount",
	loadingKey: "itemCommandsLoading",
	editionKey: "itemCommandEdition",
	readyKey: "itemCommandReady",
	onHydrate: (store, entity: ReadExtendedCommandItemDto, expand) => {
		if (expand.includes("command") && entity.command && entity.id_command) {
			useCommandsStore().commands[entity.id_command] = entity.command;
		}
		if (expand.includes("item") && entity.item && entity.id_item) {
			useItemsStore().items[entity.id_item] = entity.item;
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
		itemCommandEdition: {} as Record<string, Edition<{ create: CreateItemCommand, update: UpdateCommandItemDto }>>,
		itemCommandReady: {} as Record<string, Ready<{ create: CreateItemCommand, update: UpdateCommandItemDto }>>,
	}),
	getters: {
		// the records of a item, indexed by the rest of the key
		itemCommandsByItem: (state) => (id: string) => itemCommandResource.slice(state.itemCommands, { id_item: id }),
		itemCommandsCountByItem: (state) => (id: string) => state.itemCommandsTotalCount[itemCommandResource.toScopeKey({ id_item: id })] ?? 0,
		itemCommandsGroupedByItem: (state) => itemCommandResource.group(state.itemCommands, "id_item"),
		itemCommandEditionByItem: (state) => (id: string) => itemCommandResource.live(state.itemCommandEdition, { id_item: id }),
		itemCommandReadyByItem: (state) => (id: string) => itemCommandResource.live(state.itemCommandReady, { id_item: id }),
		// the records of a command, indexed by the rest of the key
		itemCommandsByCommand: (state) => (id: string) => itemCommandResource.slice(state.itemCommands, { id_command: id }),
		itemCommandsCountByCommand: (state) => (id: string) => state.itemCommandsTotalCount[itemCommandResource.toScopeKey({ id_command: id })] ?? 0,
		itemCommandsGroupedByCommand: (state) => itemCommandResource.group(state.itemCommands, "id_command"),
		itemCommandEditionByCommand: (state) => (id: string) => itemCommandResource.live(state.itemCommandEdition, { id_command: id }),
		itemCommandReadyByCommand: (state) => (id: string) => itemCommandResource.live(state.itemCommandReady, { id_command: id }),
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
