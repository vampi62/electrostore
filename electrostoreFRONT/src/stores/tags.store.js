import { defineStore } from "pinia";

import { createMainResource, createNestedResource } from "@/helpers";

import { useStoresStore, useItemsStore, useEquipementsStore } from "@/stores";

const EXPAND_HANDLERS = {
	stores_tags: (store, idTag, tag) => {
		store.tagsStore[idTag] = {};
		for (const tagStore of tag.stores_tags) {
			store.tagsStore[idTag][tagStore.id_store] = tagStore;
		}
	},
	boxs_tags: (store, idTag, tag) => {
		store.tagsBox[idTag] = {};
		for (const tagBox of tag.boxs_tags) {
			store.tagsBox[idTag][tagBox.id_box] = tagBox;
		}
	},
	items_tags: (store, idTag, tag) => {
		store.tagsItem[idTag] = {};
		for (const tagItem of tag.items_tags) {
			store.tagsItem[idTag][tagItem.id_item] = tagItem;
		}
	},
	equipement_tags: (store, idTag, tag) => {
		store.tagsEquipement[idTag] = {};
		for (const tagEquipement of tag.equipement_tags) {
			store.tagsEquipement[idTag][tagEquipement.id_equipement] = tagEquipement;
		}
	},
};

function hydrateTag(store, idTag, tag, expand = []) {
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

const tagResource = createMainResource({
	path: () => "/tag",
	idField: "id_tag",
	stateKey: "tags",
	countKey: "tagsTotalCount",
	loadingKey: "tagsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateTag(store, entity.id_tag, entity, expand);
	},
});

const tagStoreResource = createNestedResource({
	path: (idTag) => `/tag/${idTag}/store`,
	idField: "id_store",
	stateKey: "tagsStore",
	countKey: "tagsStoreTotalCount",
	loadingKey: "tagsStoreLoading",
	editionKey: "tagStoreEdition",
	readyKey: "tagStoreReady",
	onHydrate: (store, idTag, entity, expand) => {
		if (expand.includes("store")) {
			const storesStore = useStoresStore();
			storesStore.stores[entity.id_store] = entity.store;
		}
	},
});
const tagBoxResource = createNestedResource({
	path: (idTag) => `/tag/${idTag}/box`,
	idField: "id_box",
	stateKey: "tagsBox",
	countKey: "tagsBoxTotalCount",
	loadingKey: "tagsBoxLoading",
	editionKey: "tagBoxEdition",
	readyKey: "tagBoxReady",
	onHydrate: (store, idTag, entity, expand) => {
		if (expand.includes("box")) {
			const storesStore = useStoresStore();
			storesStore.boxs[entity.id_box] = entity.box;
		}
	},
});

const tagItemResource = createNestedResource({
	path: (idTag) => `/tag/${idTag}/item`,
	idField: "id_item",
	stateKey: "tagsItem",
	countKey: "tagsItemTotalCount",
	loadingKey: "tagsItemLoading",
	editionKey: "tagItemEdition",
	readyKey: "tagItemReady",
	onHydrate: (store, idTag, entity, expand) => {
		if (expand.includes("item")) {
			const itemsStore = useItemsStore();
			itemsStore.items[entity.id_item] = entity.item;
		}
	},
});

const tagEquipementResource = createNestedResource({
	path: (idTag) => `/tag/${idTag}/equipement`,
	idField: "id_equipement",
	stateKey: "tagsEquipement",
	countKey: "tagsEquipementTotalCount",
	loadingKey: "tagsEquipementLoading",
	editionKey: "tagEquipementEdition",
	readyKey: "tagEquipementReady",
	onHydrate: (store, idTag, entity, expand) => {
		if (expand.includes("equipement")) {
			const equipementsStore = useEquipementsStore();
			equipementsStore.equipements[entity.id_equipement] = entity.equipement;
		}
	},
});

export const useTagsStore = defineStore("tags",{
	state: () => ({
		tagsLoading: false,
		tagsTotalCount: 0,
		tags: {},
		tagEdition: {},

		tagsStoreLoading: false,
		tagsStoreTotalCount: {},
		tagsStore: {},
		tagStoreEdition: {},
		tagStoreReady: {},

		tagsBoxLoading: false,
		tagsBoxTotalCount: {},
		tagsBox: {},
		tagBoxEdition: {},
		tagBoxReady: {},

		tagsItemLoading: false,
		tagsItemTotalCount: {},
		tagsItem: {},
		tagItemEdition: {},
		tagItemReady: {},

		tagsEquipementLoading: false,
		tagsEquipementTotalCount: {},
		tagsEquipement: {},
		tagEquipementEdition: {},
		tagEquipementReady: {},
	}),
	actions: {
		getTagByList: tagResource.getByList,
		getTagByInterval: tagResource.getByInterval,
		getTagById: tagResource.getById,
		createTag: tagResource.create,
		updateTag: tagResource.update,
		deleteTag: tagResource.remove,
		createTagBulk: tagResource.createBulk,
		loadToEdition(id, preset = null) {
			this.tagEdition[id] = {};
			tagResource.loadEditionPreset(id, preset);
			if (id !== "new" && this.tags[id]) {
				this.tagEdition[id] = {
					name_tag: this.tags[id].name_tag,
					weight_tag: this.tags[id].weight_tag,
					loading: false,
				};
			} else {
				this.tagEdition[id] = {
					loading: false,
				};
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
		setLoadingEdition(id, loading) {
			if (!this.tagEdition[id]) {
				this.tagEdition[id] = {};
			}
			this.tagEdition[id].loading = loading;
		},
		clearEdition(id) {
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
		async saveAllChanges(id) {
			let realId = id;
			if (id === "new") {
				realId = await this.createTag(this.tagEdition[id]);
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
