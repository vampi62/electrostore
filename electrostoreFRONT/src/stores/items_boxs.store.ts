import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useStoresStore } from "@/stores";

import type { Ready, Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadItemBoxDto = components["schemas"]["ReadItemBoxDto"];
type ReadExtendedItemBoxDto = components["schemas"]["ReadExtendedItemBoxDto"];
type CreateItemBoxByItemDto = components["schemas"]["CreateItemBoxByItemDto"];
type UpdateItemBoxDto = components["schemas"]["UpdateItemBoxDto"];

const itemBoxResource = createResource<{ readExtended: ReadExtendedItemBoxDto, readBasic: ReadItemBoxDto, create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>({
	compositeKey: ["id_item", "id_box"],
	path: [{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/box` }],
	entityPath: [{ keys: ["id_item", "id_box"], url: ({ id_item, id_box }) => `/item/${id_item}/box/${id_box}` }],
	stateKey: "itemBoxs",
	countKey: "itemBoxsTotalCount",
	loadingKey: "itemBoxsLoading",
	editionKey: "itemBoxEdition",
	readyKey: "itemBoxReady",
	onHydrate: (store, entity: ReadExtendedItemBoxDto, expand) => {
		if (expand.includes("box") && entity.box && entity.id_box && entity.box.id_store) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.box.id_store] ??= {};
			storesStore.boxs[entity.box.id_store][entity.id_box] = entity.box;
		}
	},
});

// Records are indexed by the composite key "<id_item>:<id_box>"
export const useItemsBoxsStore = defineStore("itemsBoxs", {
	state: () => ({
		itemBoxsLoading: false,
		// indexed by scope: "id_item=3"
		itemBoxsTotalCount: {} as Record<string, number>,
		itemBoxs: {} as Record<string, ReadExtendedItemBoxDto>,
		itemBoxEdition: {} as Record<string, Edition<{ create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>>,
		itemBoxReady: {} as Record<string, Ready<{ create: CreateItemBoxByItemDto, update: UpdateItemBoxDto }>>,
	}),
	getters: {
		// the records of an item, indexed by id_box
		itemBoxsByItem: (state) => (idItem: string) => itemBoxResource.slice(state.itemBoxs, { id_item: idItem }),
		itemBoxEditionByItem: (state) => (idItem: string) => itemBoxResource.slice(state.itemBoxEdition, { id_item: idItem }),
		itemBoxReadyByItem: (state) => (idItem: string) => itemBoxResource.slice(state.itemBoxReady, { id_item: idItem }),
		itemBoxsCountByItem: (state) => (idItem: string) => state.itemBoxsTotalCount[itemBoxResource.toScopeKey({ id_item: idItem })] ?? 0,
	},
	actions: {
		getItemBoxByInterval: itemBoxResource.getByInterval,
		getItemBoxById: itemBoxResource.getById,
		createItemBox: itemBoxResource.create,
		updateItemBox: itemBoxResource.update,
		deleteItemBox: itemBoxResource.remove,
		hydrateItemBoxs: itemBoxResource.hydrate,
		setItemBoxEdition: itemBoxResource.setEdition,
		removeItemBoxEdition: itemBoxResource.removeEdition,
		removeItemBoxReady: itemBoxResource.removeReady,
		setItemBoxStagedSlice: itemBoxResource.setStagedSlice,
		valideItemBoxEditionByKey: itemBoxResource.valideEditionByKey,
		copyItemBoxByKeyPart: itemBoxResource.copyByKeyPart,
		clearItemBoxEdition: itemBoxResource.clearEdition,
		pushItemBoxChange: itemBoxResource.pushChange,
	},
});
