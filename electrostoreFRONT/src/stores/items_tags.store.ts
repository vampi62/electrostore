import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useTagsStore } from "@/stores";

import type { Ready, Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadItemTagDto = components["schemas"]["ReadItemTagDto"];
type ReadExtendedItemTagDto = components["schemas"]["ReadExtendedItemTagDto"];
type CreateItemTagByItemDto = components["schemas"]["CreateItemTagByItemDto"];
type CreateItemTagByTagDto = components["schemas"]["CreateItemTagByTagDto"];
type CreateItemTag = CreateItemTagByItemDto | CreateItemTagByTagDto;

const itemTagResource = createResource<{ readExtended: ReadExtendedItemTagDto, readBasic: ReadItemTagDto, create: CreateItemTag, createBulk: CreateItemTag[], deleteBulk: number[] }>({
	compositeKey: ["id_item", "id_tag"],
	// the first route whose keys are given is used: { id_item } => /item/{id_item}/tag, { id_tag } => /tag/{id_tag}/item
	path: [
		{ keys: ["id_item"], url: ({ id_item }) => `/item/${id_item}/tag` },
		{ keys: ["id_tag"], url: ({ id_tag }) => `/tag/${id_tag}/item` },
	],
	// a record is always addressed by its complete key, so the first route always wins: /tag/{id_tag}/item/{id_item} would never be reached
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
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
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
		// part of the key => the matching records with their composite key, e.g. itemTagsBy({ id_item: 3 })
		itemTagsBy: (state) => (part: { id_item?: string, id_tag?: string }) => itemTagResource.pick(state.itemTags, part),
		// the records of an item, indexed by id_tag
		itemTagsByItem: (state) => (idItem: string) => itemTagResource.slice(state.itemTags, { id_item: idItem }),
		itemTagEditionByItem: (state) => (idItem: string) => itemTagResource.slice(state.itemTagEdition, { id_item: idItem }),
		itemTagReadyByItem: (state) => (idItem: string) => itemTagResource.slice(state.itemTagReady, { id_item: idItem }),
		itemTagsCountByItem: (state) => (idItem: string) => state.itemTagsTotalCount[itemTagResource.toScopeKey({ id_item: idItem })] ?? 0,
		// the records of a tag, indexed by id_item
		itemTagsByTag: (state) => (idTag: string) => itemTagResource.slice(state.itemTags, { id_tag: idTag }),
		// every item with its tags: { [id_item]: { [id_tag]: record } }
		itemTagsGroupedByItem: (state) => itemTagResource.group(state.itemTags, "id_item"),
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
