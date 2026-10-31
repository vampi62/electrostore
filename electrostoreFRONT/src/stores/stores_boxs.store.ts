import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useItemsBoxsStore, useBoxsTagsStore } from "@/stores";

import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadBoxDto = components["schemas"]["ReadBoxDto"];
type ReadExtendedBoxDto = components["schemas"]["ReadExtendedBoxDto"];
type CreateBoxByStoreDto = components["schemas"]["CreateBoxByStoreDto"];
type UpdateBoxByStoreDto = components["schemas"]["UpdateBoxByStoreDto"];
type UpdateBulkBoxByStoreDto = components["schemas"]["UpdateBulkBoxByStoreDto"];

const boxResource = createResource<{ readExtended: ReadExtendedBoxDto, readBasic: ReadBoxDto, create: CreateBoxByStoreDto, update: UpdateBoxByStoreDto, createBulk: CreateBoxByStoreDto[], updateBulk: UpdateBulkBoxByStoreDto[], deleteBulk: number[] }>({
	compositeKey: ["id_store", "id_box"],
	generatedField: "id_box",
	path: [
		{ keys: ["id_store"], url: ({ id_store }) => `/store/${id_store}/box` },
	],
	entityPath: [
		{ keys: ["id_store", "id_box"], url: ({ id_store, id_box }) => `/store/${id_store}/box/${id_box}` },
	],
	stateKey: "boxs",
	countKey: "boxsTotalCount",
	loadingKey: "boxsLoading",
	editionKey: "boxEdition",
	onHydrate: (store, entity: ReadExtendedBoxDto, expand) => {
		if (entity.id_box === undefined) {
			return;
		}
		const scope = { id_box: String(entity.id_box) };
		useItemsBoxsStore().hydrateItemBoxs(scope, undefined, entity.item_boxs_count);
		useBoxsTagsStore().hydrateBoxTags(scope, undefined, entity.box_tags_count);
		if (expand.includes("item_boxs")) {
			useItemsBoxsStore().hydrateItemBoxs(scope, entity.item_boxs ?? []);
		}
		if (expand.includes("box_tags")) {
			useBoxsTagsStore().hydrateBoxTags(scope, entity.box_tags ?? []);
		}
	},
});

// Records are indexed by the composite key "<id_store>:<id_box>"
export const useStoresBoxsStore = defineStore("storesBoxs", {
	state: () => ({
		boxsLoading: false,
		// indexed by scope: "id_store=3"
		boxsTotalCount: {} as Record<string, number>,
		boxs: {} as Record<string, ReadExtendedBoxDto>,
		boxEdition: {} as Record<string, Edition<{ create: CreateBoxByStoreDto, update: UpdateBulkBoxByStoreDto }>>,
	}),
	getters: {
		// the records of a store, indexed by the rest of the key
		boxsByStore: (state) => (id: string) => boxResource.slice(state.boxs, { id_store: id }),
		boxsCountByStore: (state) => (id: string) => state.boxsTotalCount[boxResource.toScopeKey({ id_store: id })] ?? 0,
		boxEditionByStore: (state) => (id: string) => boxResource.live(state.boxEdition, { id_store: id }),
		// every box loaded, indexed by id_box
		boxsById: (state) => Object.fromEntries(Object.values(state.boxs).map((box) => [String(box.id_box), box])),
	},
	actions: {
		getBoxByInterval: boxResource.getByInterval,
		getBoxById: boxResource.getById,
		createBox: boxResource.create,
		updateBox: boxResource.update,
		deleteBox: boxResource.remove,
		createBoxBulk: boxResource.createBulk,
		updateBoxBulk: boxResource.updateBulk,
		deleteBoxBulk: boxResource.removeBulk,
		hydrateBoxs: boxResource.hydrate,
		upsertBoxs: boxResource.upsert,
		getAvailableNewBoxId: boxResource.getAvailableNewId,
		setBoxEdition: boxResource.setEdition,
		removeBoxEdition: boxResource.removeEdition,
		setBoxStagedSlice: boxResource.setStagedSlice,
		clearBoxEdition: boxResource.clearEdition,
	},
});
