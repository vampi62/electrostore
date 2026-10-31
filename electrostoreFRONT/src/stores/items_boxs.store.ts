import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useItemsStore, useStoresBoxsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadItemBoxDto = components["schemas"]["ReadItemBoxDto"];
type ReadExtendedItemBoxDto = components["schemas"]["ReadExtendedItemBoxDto"];
type CreateItemBoxByItemDto = components["schemas"]["CreateItemBoxByItemDto"];
type CreateItemBoxByBoxDto = components["schemas"]["CreateItemBoxByBoxDto"];
type UpdateItemBoxDto = components["schemas"]["UpdateItemBoxDto"];
type CreateItemBox = CreateItemBoxByItemDto | CreateItemBoxByBoxDto;

const itemBoxResource = createResource<{ readExtended: ReadExtendedItemBoxDto, readBasic: ReadItemBoxDto, create: CreateItemBox, update: UpdateItemBoxDto }>({
	compositeKey: ["id_item", "id_box"],
	routeOnlyFields: ["id_store"],
	path: [
		{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/box` },
		{ keys: ["id_store", "id_box"], url: ({ id_store, id_box }) => `/store/${id_store}/box/${id_box}/item` },
	],
	entityPath: [
		{ keys: ["id_item", "id_box"], url: ({ id_item, id_box }) => `/item/${id_item}/box/${id_box}` },
	],
	stateKey: "itemBoxs",
	countKey: "itemBoxsTotalCount",
	loadingKey: "itemBoxsLoading",
	editionKey: "itemBoxEdition",
	readyKey: "itemBoxReady",
	onHydrate: (store, entity: ReadExtendedItemBoxDto, expand) => {
		if (expand.includes("box") && entity.box) {
			useStoresBoxsStore().upsertBoxs([entity.box]);
		}
		if (expand.includes("item") && entity.item && entity.id_item) {
			useItemsStore().items[entity.id_item] = entity.item;
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
		itemBoxEdition: {} as Record<string, Edition<{ create: CreateItemBox, update: UpdateItemBoxDto }>>,
		itemBoxReady: {} as Record<string, Ready<{ create: CreateItemBox, update: UpdateItemBoxDto }>>,
	}),
	getters: {
		// the records of a item, indexed by the rest of the key
		itemBoxsByItem: (state) => (id: string) => itemBoxResource.slice(state.itemBoxs, { id_item: id }),
		itemBoxsCountByItem: (state) => (id: string) => state.itemBoxsTotalCount[itemBoxResource.toScopeKey({ id_item: id })] ?? 0,
		itemBoxEditionByItem: (state) => (id: string) => itemBoxResource.live(state.itemBoxEdition, { id_item: id }),
		itemBoxReadyByItem: (state) => (id: string) => itemBoxResource.live(state.itemBoxReady, { id_item: id }),
		// the records of a box, indexed by the rest of the key
		itemBoxsByBox: (state) => (id: string) => itemBoxResource.slice(state.itemBoxs, { id_box: id }),
		itemBoxsCountByBox: (state) => (id: string) => state.itemBoxsTotalCount[itemBoxResource.toScopeKey({ id_box: id })] ?? 0,
		itemBoxEditionByBox: (state) => (id: string) => itemBoxResource.live(state.itemBoxEdition, { id_box: id }),
		itemBoxReadyByBox: (state) => (id: string) => itemBoxResource.live(state.itemBoxReady, { id_box: id }),
	},
	actions: {
		getItemBoxByInterval: itemBoxResource.getByInterval,
		getItemBoxById: itemBoxResource.getById,
		createItemBox: itemBoxResource.create,
		updateItemBox: itemBoxResource.update,
		deleteItemBox: itemBoxResource.remove,
		hydrateItemBoxs: itemBoxResource.hydrate,
		upsertItemBoxs: itemBoxResource.upsert,
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
