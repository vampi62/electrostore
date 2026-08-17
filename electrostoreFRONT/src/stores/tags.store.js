import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useStoresStore, useItemsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

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
};

function hydrateTag(store, idTag, tag, expand = []) {
	store.tagsStoreTotalCount[idTag] = tag.stores_tags_count;
	store.tagsBoxTotalCount[idTag] = tag.boxs_tags_count;
	store.tagsItemTotalCount[idTag] = tag.items_tags_count;
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
	onHydrate: (store, idTag, entity, expand) => {
		if (expand.includes("item")) {
			const itemsStore = useItemsStore();
			itemsStore.items[entity.id_item] = entity.item;
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

		tagsBoxLoading: false,
		tagsBoxTotalCount: {},
		tagsBox: {},
		tagBoxEdition: {},

		tagsItemLoading: false,
		tagsItemTotalCount: {},
		tagsItem: {},
		tagItemEdition: {},
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
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.tagEdition[id][key] = value;
					}
				});
			}
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
			this.tagStoreEdition[id] = {};
			this.tagBoxEdition[id] = {};
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
			delete this.tagStoreEdition[id];
			delete this.tagBoxEdition[id];
		},

		getTagStoreByInterval: tagStoreResource.getByInterval,
		getTagStoreById: tagStoreResource.getById,
		createTagStore: tagStoreResource.create,
		deleteTagStore: tagStoreResource.remove,
		createTagStoreBulk: tagStoreResource.createBulk,
		deleteTagStoreBulk: tagStoreResource.removeBulk,

		getTagBoxByInterval: tagBoxResource.getByInterval,
		getTagBoxById: tagBoxResource.getById,
		createTagBox: tagBoxResource.create,
		deleteTagBox: tagBoxResource.remove,
		createTagBoxBulk: tagBoxResource.createBulk,
		deleteTagBoxBulk: tagBoxResource.removeBulk,

		getTagItemByInterval: tagItemResource.getByInterval,
		getTagItemById: tagItemResource.getById,
		createTagItem: tagItemResource.create,
		deleteTagItem: tagItemResource.remove,
		createTagItemBulk: tagItemResource.createBulk,
		deleteTagItemBulk: tagItemResource.removeBulk,
	},
});
