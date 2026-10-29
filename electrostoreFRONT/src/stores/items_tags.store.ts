import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useTagsStore, useItemsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadItemTagDto = components["schemas"]["ReadItemTagDto"];
type ReadExtendedItemTagDto = components["schemas"]["ReadExtendedItemTagDto"];
type CreateItemTagByItemDto = components["schemas"]["CreateItemTagByItemDto"];
type CreateItemTagByTagDto = components["schemas"]["CreateItemTagByTagDto"];
type CreateItemTag = CreateItemTagByItemDto | CreateItemTagByTagDto;

const itemTagResource = createResource<{ readExtended: ReadExtendedItemTagDto, readBasic: ReadItemTagDto, create: CreateItemTag, createBulk: CreateItemTag[], deleteBulk: number[] }>({
	compositeKey: ["id_item", "id_tag"],
	path: [
		{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/tag` },
		{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}/item` },
	],
	entityPath: [
		{ keys: ["id_item", "id_tag"], url: ({ id_item, id_tag }) => `/item/${id_item}/tag/${id_tag}` },
	],
	stateKey: "itemTags",
	countKey: "itemTagsTotalCount",
	loadingKey: "itemTagsLoading",
	editionKey: "itemTagEdition",
	readyKey: "itemTagReady",
	onHydrate: (store, entity: ReadExtendedItemTagDto, expand) => {
		if (expand.includes("tag") && entity.tag && entity.id_tag) {
			useTagsStore().tags[entity.id_tag] = entity.tag;
		}
		if (expand.includes("item") && entity.item && entity.id_item) {
			useItemsStore().items[entity.id_item] = entity.item;
		}
	},
});

// Records are indexed by the composite key "<id_item>:<id_tag>"
export const useItemsTagsStore = defineStore("itemsTags", {
	state: () => ({
		itemTagsLoading: false,
		// indexed by scope: "id_item=3"
		itemTagsTotalCount: {} as Record<string, number>,
		itemTags: {} as Record<string, ReadItemTagDto>,
		itemTagEdition: {} as Record<string, Edition<{ create: CreateItemTag }>>,
		itemTagReady: {} as Record<string, Ready<{ create: CreateItemTag }>>,
	}),
	getters: {
		// the records of a item, indexed by the rest of the key
		itemTagsByItem: (state) => (id: string) => itemTagResource.slice(state.itemTags, { id_item: id }),
		itemTagsCountByItem: (state) => (id: string) => state.itemTagsTotalCount[itemTagResource.toScopeKey({ id_item: id })] ?? 0,
		itemTagsGroupedByItem: (state) => itemTagResource.group(state.itemTags, "id_item"),
		itemTagEditionByItem: (state) => (id: string) => itemTagResource.live(state.itemTagEdition, { id_item: id }),
		itemTagReadyByItem: (state) => (id: string) => itemTagResource.live(state.itemTagReady, { id_item: id }),
		// the records of a tag, indexed by the rest of the key
		itemTagsByTag: (state) => (id: string) => itemTagResource.slice(state.itemTags, { id_tag: id }),
		itemTagsCountByTag: (state) => (id: string) => state.itemTagsTotalCount[itemTagResource.toScopeKey({ id_tag: id })] ?? 0,
		itemTagsGroupedByTag: (state) => itemTagResource.group(state.itemTags, "id_tag"),
		itemTagEditionByTag: (state) => (id: string) => itemTagResource.live(state.itemTagEdition, { id_tag: id }),
		itemTagReadyByTag: (state) => (id: string) => itemTagResource.live(state.itemTagReady, { id_tag: id }),
	},
	actions: {
		getItemTagByInterval: itemTagResource.getByInterval,
		getItemTagById: itemTagResource.getById,
		createItemTag: itemTagResource.create,
		deleteItemTag: itemTagResource.remove,
		createItemTagBulk: itemTagResource.createBulk,
		deleteItemTagBulk: itemTagResource.removeBulk,
		hydrateItemTags: itemTagResource.hydrate,
		setItemTagEdition: itemTagResource.setEdition,
		removeItemTagEdition: itemTagResource.removeEdition,
		removeItemTagReady: itemTagResource.removeReady,
		setItemTagStagedSlice: itemTagResource.setStagedSlice,
		valideItemTagEditionByKey: itemTagResource.valideEditionByKey,
		copyItemTagByKeyPart: itemTagResource.copyByKeyPart,
		clearItemTagEdition: itemTagResource.clearEdition,
		pushItemTagChange: itemTagResource.pushChange,
	},
});
