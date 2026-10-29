import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useStoresStore, useTagsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadStoreTagDto = components["schemas"]["ReadStoreTagDto"];
type ReadExtendedStoreTagDto = components["schemas"]["ReadExtendedStoreTagDto"];
type CreateStoreTagByStoreDto = components["schemas"]["CreateStoreTagByStoreDto"];
type CreateStoreTagByTagDto = components["schemas"]["CreateStoreTagByTagDto"];
type CreateStoreTag = CreateStoreTagByStoreDto | CreateStoreTagByTagDto;

const storeTagResource = createResource<{ readExtended: ReadExtendedStoreTagDto, readBasic: ReadStoreTagDto, create: CreateStoreTag, createBulk: CreateStoreTag[], deleteBulk: number[] }>({
	compositeKey: ["id_store", "id_tag"],
	path: [
		{ keys: ["id_store"], url: ({ id_store }) => `/store/${id_store}/tag` },
		{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}/store` },
	],
	entityPath: [
		{ keys: ["id_store", "id_tag"], url: ({ id_store, id_tag }) => `/store/${id_store}/tag/${id_tag}` },
	],
	stateKey: "storeTags",
	countKey: "storeTagsTotalCount",
	loadingKey: "storeTagsLoading",
	editionKey: "storeTagEdition",
	readyKey: "storeTagReady",
	onHydrate: (store, entity: ReadExtendedStoreTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			useTagsStore().tags[entity.id_tag] = entity.tag;
		}
		if (expand.includes("store") && entity.store && entity.id_store) {
			useStoresStore().stores[entity.id_store] = entity.store;
		}
	},
});

// Records are indexed by the composite key "<id_store>:<id_tag>"
export const useStoresTagsStore = defineStore("storesTags", {
	state: () => ({
		storeTagsLoading: false,
		// indexed by scope: "id_store=3"
		storeTagsTotalCount: {} as Record<string, number>,
		storeTags: {} as Record<string, ReadStoreTagDto>,
		storeTagEdition: {} as Record<string, Edition<{ create: CreateStoreTag }>>,
		storeTagReady: {} as Record<string, Ready<{ create: CreateStoreTag }>>,
	}),
	getters: {
		// the records of a store, indexed by the rest of the key
		storeTagsByStore: (state) => (id: string) => storeTagResource.slice(state.storeTags, { id_store: id }),
		storeTagsCountByStore: (state) => (id: string) => state.storeTagsTotalCount[storeTagResource.toScopeKey({ id_store: id })] ?? 0,
		storeTagsGroupedByStore: (state) => storeTagResource.group(state.storeTags, "id_store"),
		storeTagEditionByStore: (state) => (id: string) => storeTagResource.live(state.storeTagEdition, { id_store: id }),
		storeTagReadyByStore: (state) => (id: string) => storeTagResource.live(state.storeTagReady, { id_store: id }),
		// the records of a tag, indexed by the rest of the key
		storeTagsByTag: (state) => (id: string) => storeTagResource.slice(state.storeTags, { id_tag: id }),
		storeTagsCountByTag: (state) => (id: string) => state.storeTagsTotalCount[storeTagResource.toScopeKey({ id_tag: id })] ?? 0,
		storeTagsGroupedByTag: (state) => storeTagResource.group(state.storeTags, "id_tag"),
		storeTagEditionByTag: (state) => (id: string) => storeTagResource.live(state.storeTagEdition, { id_tag: id }),
		storeTagReadyByTag: (state) => (id: string) => storeTagResource.live(state.storeTagReady, { id_tag: id }),
	},
	actions: {
		getStoreTagByInterval: storeTagResource.getByInterval,
		getStoreTagById: storeTagResource.getById,
		createStoreTag: storeTagResource.create,
		deleteStoreTag: storeTagResource.remove,
		createStoreTagBulk: storeTagResource.createBulk,
		deleteStoreTagBulk: storeTagResource.removeBulk,
		hydrateStoreTags: storeTagResource.hydrate,
		setStoreTagEdition: storeTagResource.setEdition,
		removeStoreTagEdition: storeTagResource.removeEdition,
		removeStoreTagReady: storeTagResource.removeReady,
		setStoreTagStagedSlice: storeTagResource.setStagedSlice,
		valideStoreTagEditionByKey: storeTagResource.valideEditionByKey,
		copyStoreTagByKeyPart: storeTagResource.copyByKeyPart,
		clearStoreTagEdition: storeTagResource.clearEdition,
		pushStoreTagChange: storeTagResource.pushChange,
	},
});
