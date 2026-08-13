import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useTagsStore, useItemsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS_STORE = {
	boxs: (store, idStore, storeData) => {
		store.boxs[idStore] = {};
		for (const box of storeData.boxs) {
			store.boxs[idStore][box.id_box] = box;
		}
	},
	leds: (store, idStore, storeData) => {
		store.leds[idStore] = {};
		for (const led of storeData.leds) {
			store.leds[idStore][led.id_led] = led;
		}
	},
	stores_tags: (store, idStore, storeData) => {
		store.storeTags[idStore] = {};
		for (const tag of storeData.stores_tags) {
			store.storeTags[idStore][tag.id_tag] = tag;
		}
	},
};
const EXPAND_HANDLERS_BOX = {
	item_boxs: (store, idBox, boxData) => {
		store.boxItems[idBox] = {};
		for (const item of boxData.item_boxs) {
			store.boxItems[idBox][item.id_item] = item;
		}
	},
	box_tags: (store, idBox, boxData) => {
		store.boxTags[idBox] = {};
		for (const tag of boxData.box_tags) {
			store.boxTags[idBox][tag.id_tag] = tag;
		}
	},
};

function hydrateStore(store, idStore, storeData, expand = []) {
	store.boxsTotalCount[idStore] = storeData.boxs_count;
	store.ledsTotalCount[idStore] = storeData.leds_count;
	store.storeTagsTotalCount[idStore] = storeData.stores_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_STORE[key]) {
			EXPAND_HANDLERS_STORE[key](store, idStore, storeData);
		}
	}
}

function hydrateBox(store, idStore, idBox, boxData, expand = []) {
	store.boxs[idStore][idBox] = boxData;
	store.boxItemsTotalCount[idBox] = boxData.item_boxs_count;
	store.boxTagsTotalCount[idBox] = boxData.box_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS_BOX[key]) {
			EXPAND_HANDLERS_BOX[key](store, idBox, boxData);
		}
	}
}

const storeResource = createMainResource({
	path: () => "/store",
	idField: "id_store",
	stateKey: "stores",
	countKey: "storesTotalCount",
	loadingKey: "storesLoading",
	onHydrate: (store, entity, expand) => {
		hydrateStore(store, entity.id_store, entity, expand);
	},
});

const boxResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/box`,
	idField: "id_box",
	stateKey: "boxs",
	countKey: "boxsTotalCount",
	loadingKey: "boxsLoading",
	onHydrate: (store, idStore, entity, expand) => {
		hydrateBox(store, idStore, entity.id_box, entity, expand);
	},
});
const ledResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/led`,
	idField: "id_led",
	stateKey: "leds",
	countKey: "ledsTotalCount",
	loadingKey: "ledsLoading",
});
const storeTagResource = createNestedResource({
	path: (idStore) => `/store/${idStore}/store_tag`,
	idField: "id_tag",
	stateKey: "storeTags",
	countKey: "storeTagsTotalCount",
	loadingKey: "storeTagsLoading",
	onHydrate: (store, idStore, entity, expand) => {
		if (expand.includes("tag")) {
			const tagsStore = useTagsStore();
			tagsStore.tags[entity.id_tag] = entity.tag;
		}
	},
});

export const useStoresStore = defineStore("stores",{
	state: () => ({
		storesLoading: false,
		storesTotalCount: 0,
		stores: {},
		storeEdition: {},

		boxsLoading: false,
		boxsTotalCount: {},
		boxs: {},
		boxEdition: {},

		ledsLoading: false,
		ledsTotalCount: {},
		leds: {},
		ledEdition: {},

		storeTagsLoading: false,
		storeTagsTotalCount: {},
		storeTags: {},
		storeTagEdition: {},

		boxItemsLoading: false,
		boxItemsTotalCount: {},
		boxItems: {},
		boxItemEdition: {},

		boxTagsLoading: false,
		boxTagsTotalCount: {},
		boxTags: {},
		boxTagEdition: {},
	}),
	actions: {
		getStoreByList: storeResource.getByList,
		getStoreByInterval: storeResource.getByInterval,
		getStoreById: storeResource.getById,
		createStore: storeResource.create,
		updateStore: storeResource.update,
		deleteStore: storeResource.remove,
		async createStoreComplete(id, params) {
			const store = await fetchWrapper.post({
				url: `${baseUrl}/store/complete`,
				useToken: "access",
				body: params,
			});
			this.stores[store.store.id_store] = store.store;
			return store.store.id_store;
		},
		async updateStoreComplete(id, params) {
			this.stores[id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${id}/complete`,
				useToken: "access",
				body: params,
			});
		},
		loadToEdition(id, preset = null) {
			this.storeEdition[id] = {};
			if (preset) {
				this.storeEdition[id] = {};
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.storeEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.stores[id]) {
				this.storeEdition[id] = {
					loading: false,
					id_store: this.stores[id].id_store,
					nom_store: this.stores[id].nom_store,
					mqtt_name_store: this.stores[id].mqtt_name_store,
					xlength_store: this.stores[id].xlength_store,
					ylength_store: this.stores[id].ylength_store,
					is_mqtt_connected_store: this.stores[id].is_mqtt_connected_store,
					mqtt_last_seen_store: this.stores[id].mqtt_last_seen_store,
				};
				this.ledEdition[id] = { ...this.leds[id] };
				this.boxEdition[id] = { ...this.boxs[id] };
			} else {
				this.storeEdition[id] = {
					loading: false,
				};
				this.ledEdition[id] = {};
				this.boxEdition[id] = {};
			}
		},
		setLoadingEdition(id, loading) {
			if (!this.storeEdition[id]) {
				this.storeEdition[id] = {};
			}
			this.storeEdition[id].loading = loading;
		},
		clearEdition(id) {
			if (this.storeEdition && this.storeEdition[id]) {
				delete this.storeEdition[id];
				delete this.ledEdition[id];
				delete this.boxEdition[id];
			}
		},

		getBoxByInterval: boxResource.getByInterval,
		getBoxById: boxResource.getById,
		createBox: boxResource.create,
		updateBox: boxResource.update,
		deleteBox: boxResource.remove,
		createBoxBulk: boxResource.createBulk,
		updateBoxBulk: boxResource.updateBulk,
		deleteBoxBulk: boxResource.removeBulk,
		async showBoxById(idStore, id, params) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		getLedByInterval: ledResource.getByInterval,
		getLedById: ledResource.getById,
		createLed: ledResource.create,
		updateLed: ledResource.update,
		deleteLed: ledResource.remove,
		createLedBulk: ledResource.createBulk,
		updateLedBulk: ledResource.updateBulk,
		deleteLedBulk: ledResource.removeBulk,
		async showLedById(idStore, id, params) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		getTagStoreByInterval: storeTagResource.getByInterval,
		getTagStoreById: storeTagResource.getById,
		createTagStore: storeTagResource.create,
		deleteTagStore: storeTagResource.remove,
		createTagStoreBulk: storeTagResource.createBulk,
		deleteTagStoreBulk: storeTagResource.removeBulk,

		async getBoxItemByInterval(idStore, idBox, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxItems[idBox] || clear) {
				this.boxItems[idBox] = {};
			}
			this.boxItemsLoading = true;
			const itemsStore = useItemsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.boxItems[idBox][item.id_item] = item;
				if (expand.includes("item")) {
					itemsStore.items[item.id_item] = item.item;
				}
			}
			this.boxItemsTotalCount[idBox] = newItemList["pagination"]?.["total"] || 0;
			this.boxItemsLoading = false;
			return [newItemList["pagination"]?.["nextOffset"] || 0, newItemList["pagination"]?.["hasMore"] || false];
		},
		async getBoxItemById(idStore, idBox, id, expand = []) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			if (!this.boxItems[idBox][id]) {
				this.boxItems[idBox][id] = {};
			}
			this.boxItems[idBox][id].loading = true;
			const itemsStore = useItemsStore();
			const paramString = buildQuery({ expand });
			this.boxItems[idBox][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}?${paramString}`,
				useToken: "access",
			});
			if (expand.includes("item")) {
				itemsStore.items[id] = this.boxItems[idBox][id].item;
			}
		},
		async createBoxItem(idStore, idBox, params) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			const boxItem = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item`,
				useToken: "access",
				body: params,
			});
			this.boxItems[idBox][boxItem.id_item] = boxItem;
		},
		async updateBoxItem(idStore, idBox, id, params) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			this.boxItems[idBox][id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteBoxItem(idStore, idBox, id) {
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
			});
			delete this.boxItems[idBox][id];
		},

		async getBoxTagByInterval(idStore, idBox, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxTags[idBox] || clear) {
				this.boxTags[idBox] = {};
			}
			this.boxTagsLoading = true;
			const tagsStore = useTagsStore();
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newTagList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
				if (expand.includes("tag")) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.boxTagsTotalCount[idBox] = newTagList["pagination"]?.["total"] || 0;
			this.boxTagsLoading = false;
			return [newTagList["pagination"]?.["nextOffset"] || 0, newTagList["pagination"]?.["hasMore"] || false];
		},
		async getBoxTagById(idStore, idBox, id, expand = []) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			if (!this.boxTags[idBox][id]) {
				this.boxTags[idBox][id] = {};
			}
			this.boxTags[idBox][id].loading = true;
			const tagsStore = useTagsStore();
			const paramString = buildQuery({ expand });
			this.boxTags[idBox][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}?${paramString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[id] = this.boxTags[idBox][id].tag;
			}
		},
		async createBoxTag(idStore, idBox, params) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTag = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag`,
				useToken: "access",
				body: params,
			});
			this.boxTags[idBox][boxTag.id_tag] = boxTag;
		},
		async deleteBoxTag(idStore, idBox, id) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}`,
				useToken: "access",
			});
			delete this.boxTags[idBox][id];
		},
		async createBoxTagBulk(idStore, idBox, params) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTagBulk = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk["valide"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
			}
		},
		async deleteBoxTagBulk(idStore, idBox, params) {
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			const boxTagBulk = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagBulk["valide"]) {
				delete this.boxTags[idBox][tag.id_tag];
			}
		},
	},
});
