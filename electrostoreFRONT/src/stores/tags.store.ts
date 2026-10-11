import { defineStore } from "pinia";

import { createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useStoresStore, useItemsStore, useEquipementsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Ready, Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadTagDto = components["schemas"]["ReadTagDto"];
type ReadExtendedTagDto = components["schemas"]["ReadExtendedTagDto"];
type CreateTagDto = components["schemas"]["CreateTagDto"];
type UpdateTagDto = components["schemas"]["UpdateTagDto"];

type ReadStoreTagDto = components["schemas"]["ReadStoreTagDto"];
type ReadExtendedStoreTagDto = components["schemas"]["ReadExtendedStoreTagDto"];
type CreateStoreTagByTagDto = components["schemas"]["CreateStoreTagByTagDto"];

type ReadBoxTagDto = components["schemas"]["ReadBoxTagDto"];
type ReadExtendedBoxTagDto = components["schemas"]["ReadExtendedBoxTagDto"];
type CreateBoxTagByTagDto = components["schemas"]["CreateBoxTagByTagDto"];

type ReadItemTagDto = components["schemas"]["ReadItemTagDto"];
type ReadExtendedItemTagDto = components["schemas"]["ReadExtendedItemTagDto"];
type CreateItemTagByTagDto = components["schemas"]["CreateItemTagByTagDto"];

type ReadEquipementTagDto = components["schemas"]["ReadEquipementTagDto"];
type ReadExtendedEquipementTagDto = components["schemas"]["ReadExtendedEquipementTagDto"];
type CreateEquipementTagByTagDto = components["schemas"]["CreateEquipementTagByTagDto"];

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idTag: string, data: ReadExtendedTagDto) => void> = {
	stores_tags: (store, idTag, tag) => {
		store.tagsStore[idTag] = {};
		for (const tagStore of tag.stores_tags ?? []) {
			if (!tagStore.id_store) {
				continue;
			}
			store.tagsStore[idTag][tagStore.id_store] = tagStore;
		}
	},
	boxs_tags: (store, idTag, tag) => {
		store.tagsBox[idTag] = {};
		for (const tagBox of tag.boxs_tags ?? []) {
			if (!tagBox.id_box) {
				continue;
			}
			store.tagsBox[idTag][tagBox.id_box] = tagBox;
		}
	},
	items_tags: (store, idTag, tag) => {
		store.tagsItem[idTag] = {};
		for (const tagItem of tag.items_tags ?? []) {
			if (!tagItem.id_item) {
				continue;
			}
			store.tagsItem[idTag][tagItem.id_item] = tagItem;
		}
	},
	equipement_tags: (store, idTag, tag) => {
		store.tagsEquipement[idTag] = {};
		for (const tagEquipement of tag.equipement_tags ?? []) {
			if (!tagEquipement.id_equipement) {
				continue;
			}
			store.tagsEquipement[idTag][tagEquipement.id_equipement] = tagEquipement;
		}
	},
};

function hydrateTag(store: StoreGeneric, idTag: string, tag: ReadExtendedTagDto, expand: string[] = []) {
	store.tagsStoreTotalCount[idTag] = tag.stores_tags_count;
	store.tagsBoxTotalCount[idTag] = tag.boxs_tags_count;
	store.tagsItemTotalCount[idTag] = tag.items_tags_count;
	store.tagsEquipementTotalCount[idTag] = tag.equipement_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idTag, tag);
		}
	}
}

const tagResource = createMainResource<{ readExtended: ReadExtendedTagDto, readBasic: ReadTagDto, create: CreateTagDto, update: UpdateTagDto, createBulk: CreateTagDto[] }>({
	path: () => "/tag",
	idField: "id_tag",
	stateKey: "tags",
	countKey: "tagsTotalCount",
	loadingKey: "tagsLoading",
	editionKey: "tagEdition",
	onHydrate: (store, entity: ReadExtendedTagDto, expand) => {
		if (entity.id_tag === undefined) {
			return;
		}
		hydrateTag(store, String(entity.id_tag), entity, expand);
	},
});

const tagStoreResource = createNestedResource<{ readExtended: ReadExtendedStoreTagDto, readBasic: ReadStoreTagDto, create: CreateStoreTagByTagDto }>({
	path: (idTag) => `/tag/${idTag}/store`,
	idField: "id_store",
	stateKey: "tagsStore",
	countKey: "tagsStoreTotalCount",
	loadingKey: "tagsStoreLoading",
	editionKey: "tagStoreEdition",
	readyKey: "tagStoreReady",
	onHydrate: (store, entity: ReadExtendedStoreTagDto, expand) => {
		if (expand.includes("store") && entity.store && entity.id_store) {
			const storesStore = useStoresStore();
			storesStore.stores[entity.id_store] = entity.store;
		}
	},
});
const tagBoxResource = createNestedResource<{ readExtended: ReadExtendedBoxTagDto, readBasic: ReadBoxTagDto, create: CreateBoxTagByTagDto }>({
	path: (idTag) => `/tag/${idTag}/box`,
	idField: "id_box",
	stateKey: "tagsBox",
	countKey: "tagsBoxTotalCount",
	loadingKey: "tagsBoxLoading",
	editionKey: "tagBoxEdition",
	readyKey: "tagBoxReady",
	onHydrate: (store, entity: ReadExtendedBoxTagDto, expand) => {
		if (expand.includes("box") && entity.box && entity.id_box && entity.box.id_store) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.box.id_store] ??= {};
			storesStore.boxs[entity.box.id_store][entity.id_box] = entity.box;
		}
	},
});

const tagItemResource = createNestedResource<{ readExtended: ReadExtendedItemTagDto, readBasic: ReadItemTagDto, create: CreateItemTagByTagDto }>({
	path: (idTag) => `/tag/${idTag}/item`,
	idField: "id_item",
	stateKey: "tagsItem",
	countKey: "tagsItemTotalCount",
	loadingKey: "tagsItemLoading",
	editionKey: "tagItemEdition",
	readyKey: "tagItemReady",
	onHydrate: (store, entity: ReadExtendedItemTagDto, expand) => {
		if (expand.includes("item") && entity.item && entity.id_item) {
			const itemsStore = useItemsStore();
			itemsStore.items[entity.id_item] = entity.item;
		}
	},
});

const tagEquipementResource = createNestedResource<{ readExtended: ReadExtendedEquipementTagDto, readBasic: ReadEquipementTagDto, create: CreateEquipementTagByTagDto }>({
	path: (idTag) => `/tag/${idTag}/equipement`,
	idField: "id_equipement",
	stateKey: "tagsEquipement",
	countKey: "tagsEquipementTotalCount",
	loadingKey: "tagsEquipementLoading",
	editionKey: "tagEquipementEdition",
	readyKey: "tagEquipementReady",
	onHydrate: (store, entity: ReadExtendedEquipementTagDto, expand) => {
		if (expand.includes("equipement") && entity.equipement && entity.id_equipement) {
			const equipementsStore = useEquipementsStore();
			equipementsStore.equipements[entity.id_equipement] = entity.equipement;
		}
	},
});

export const useTagsStore = defineStore("tags",{
	state: () => ({
		tagsLoading: false,
		tagsTotalCount: 0,
		tags: {} as Record<string, ReadTagDto>,
		tagEdition: {} as Record<string, Edition<{ create: CreateTagDto, update: UpdateTagDto }>>,

		tagsStoreLoading: false,
		tagsStoreTotalCount: {} as Record<string, number>,
		tagsStore: {} as Record<string, Record<string, ReadStoreTagDto>>,
		tagStoreEdition: {} as Record<string, Record<string, Edition<{ create: CreateStoreTagByTagDto }>>>,
		tagStoreReady: {} as Record<string, Record<string, Ready<{ create: CreateStoreTagByTagDto }>>>,

		tagsBoxLoading: false,
		tagsBoxTotalCount: {} as Record<string, number>,
		tagsBox: {} as Record<string, Record<string, ReadBoxTagDto>>,
		tagBoxEdition: {} as Record<string, Record<string, Edition<{ create: CreateBoxTagByTagDto }>>>,
		tagBoxReady: {} as Record<string, Record<string, Ready<{ create: CreateBoxTagByTagDto }>>>,

		tagsItemLoading: false,
		tagsItemTotalCount: {} as Record<string, number>,
		tagsItem: {} as Record<string, Record<string, ReadItemTagDto>>,
		tagItemEdition: {} as Record<string, Record<string, Edition<{ create: CreateItemTagByTagDto }>>>,
		tagItemReady: {} as Record<string, Record<string, Ready<{ create: CreateItemTagByTagDto }>>>,

		tagsEquipementLoading: false,
		tagsEquipementTotalCount: {} as Record<string, number>,
		tagsEquipement: {} as Record<string, Record<string, ReadEquipementTagDto>>,
		tagEquipementEdition: {} as Record<string, Record<string, Edition<{ create: CreateEquipementTagByTagDto }>>>,
		tagEquipementReady: {} as Record<string, Record<string, Ready<{ create: CreateEquipementTagByTagDto }>>>,
	}),
	actions: {
		getTagByList: tagResource.getByList,
		getTagByInterval: tagResource.getByInterval,
		getTagById: tagResource.getById,
		createTag: tagResource.create,
		getAvailableNewTagId: tagResource.getAvailableNewId,
		updateTag: tagResource.update,
		deleteTag: tagResource.remove,
		createTagBulk: tagResource.createBulk,
		loadToEdition(id: string, preset: string | null = null) {
			if (!isNewId(id) && this.tags[id]) {
				this.tagEdition[id] = {
					name_tag: this.tags[id].name_tag,
					weight_tag: this.tags[id].weight_tag,
					loading: false,
				};
			} else {
				this.tagEdition[id] = {
					loading: false,
				};
				tagResource.loadEditionPreset.call(this, id, preset);
			}
			this.tagItemEdition[id] = {};
			this.tagItemReady[id] = {};
			this.tagStoreEdition[id] = {};
			this.tagStoreReady[id] = {};
			this.tagBoxEdition[id] = {};
			this.tagBoxReady[id] = {};
			this.tagEquipementEdition[id] = {};
			this.tagEquipementReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.tagEdition[id]) {
				this.tagEdition[id] = {};
			}
			this.tagEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.tagEdition[id];
			delete this.tagItemEdition[id];
			delete this.tagItemReady[id];
			delete this.tagStoreEdition[id];
			delete this.tagStoreReady[id];
			delete this.tagBoxEdition[id];
			delete this.tagBoxReady[id];
			delete this.tagEquipementEdition[id];
			delete this.tagEquipementReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createTag(this.tagEdition[id] as CreateTagDto);
				this.copyTagStoreAllId(id, realId);
				this.copyTagBoxAllId(id, realId);
				this.copyTagItemAllId(id, realId);
				this.copyTagEquipementAllId(id, realId);
			} else {
				await this.updateTag(id, this.tagEdition[id]);
			}
			await Promise.all([
				this.pushTagStoreChange(realId),
				this.pushTagBoxChange(realId),
				this.pushTagItemChange(realId),
				this.pushTagEquipementChange(realId),
			]);
			return realId;
		},

		getTagStoreByInterval: tagStoreResource.getByInterval,
		getTagStoreById: tagStoreResource.getById,
		createTagStore: tagStoreResource.create,
		deleteTagStore: tagStoreResource.remove,
		createTagStoreBulk: tagStoreResource.createBulk,
		deleteTagStoreBulk: tagStoreResource.removeBulk,
		getAvailableNewTagStoreId: tagStoreResource.getAvailableNewId,
		valideTagStoreEditionById: tagStoreResource.valideEditionById,
		copyTagStorePerId: tagStoreResource.copyPerId,
		copyTagStoreAllId: tagStoreResource.copyAllId,
		pushTagStoreChange: tagStoreResource.pushChange,

		getTagBoxByInterval: tagBoxResource.getByInterval,
		getTagBoxById: tagBoxResource.getById,
		createTagBox: tagBoxResource.create,
		deleteTagBox: tagBoxResource.remove,
		createTagBoxBulk: tagBoxResource.createBulk,
		deleteTagBoxBulk: tagBoxResource.removeBulk,
		getAvailableNewTagBoxId: tagBoxResource.getAvailableNewId,
		valideTagBoxEditionById: tagBoxResource.valideEditionById,
		copyTagBoxPerId: tagBoxResource.copyPerId,
		copyTagBoxAllId: tagBoxResource.copyAllId,
		pushTagBoxChange: tagBoxResource.pushChange,

		getTagItemByInterval: tagItemResource.getByInterval,
		getTagItemById: tagItemResource.getById,
		createTagItem: tagItemResource.create,
		deleteTagItem: tagItemResource.remove,
		createTagItemBulk: tagItemResource.createBulk,
		deleteTagItemBulk: tagItemResource.removeBulk,
		getAvailableNewTagItemId: tagItemResource.getAvailableNewId,
		valideTagItemEditionById: tagItemResource.valideEditionById,
		copyTagItemPerId: tagItemResource.copyPerId,
		copyTagItemAllId: tagItemResource.copyAllId,
		pushTagItemChange: tagItemResource.pushChange,

		getTagEquipementByInterval: tagEquipementResource.getByInterval,
		getTagEquipementById: tagEquipementResource.getById,
		createTagEquipement: tagEquipementResource.create,
		deleteTagEquipement: tagEquipementResource.remove,
		createTagEquipementBulk: tagEquipementResource.createBulk,
		deleteTagEquipementBulk: tagEquipementResource.removeBulk,
		getAvailableNewTagEquipementId: tagEquipementResource.getAvailableNewId,
		valideTagEquipementEditionById: tagEquipementResource.valideEditionById,
		copyTagEquipementPerId: tagEquipementResource.copyPerId,
		copyTagEquipementAllId: tagEquipementResource.copyAllId,
		pushTagEquipementChange: tagEquipementResource.pushChange,
	},
});
