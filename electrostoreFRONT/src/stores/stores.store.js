import { defineStore } from "pinia";

import { fetchWrapper, buildQuery } from "@/helpers";

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
	store.stores[idStore] = storeData;
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
		async getStoreByList(idResearch = [], expand = []) {
			this.storesLoading = true;
			const paramString = buildQuery({ idResearch, expand });
			const newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?${paramString}`,
				useToken: "access",
			});
			for (const store of newStoreList["data"]) {
				hydrateStore(this, store.id_store, store, expand);
			}
			this.storesLoading = false;
		},
		async getStoreByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.storesLoading = true;
			if (clear) {
				this.stores = {};
			}
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?${paramString}`,
				useToken: "access",
			});
			for (const store of newStoreList["data"]) {
				hydrateStore(this, store.id_store, store, expand);
			}
			this.storesTotalCount = newStoreList["pagination"]?.["total"] || 0;
			this.storesLoading = false;
			return [newStoreList["pagination"]?.["nextOffset"] || 0, newStoreList["pagination"]?.["hasMore"] || false];
		},
		async getStoreById(id, expand = []) {
			if (!this.stores[id]) {
				this.stores[id] = {};
			}
			this.stores[id].loading = true;
			const paramString = buildQuery({ expand });
			let store = await fetchWrapper.get({
				url: `${baseUrl}/store/${id}?${paramString}`,
				useToken: "access",
			});
			hydrateStore(this, store.id_store, store, expand);
		},
		async createStore(params) {
			const store = await fetchWrapper.post({
				url: `${baseUrl}/store`,
				useToken: "access",
				body: params,
			});
			this.stores[store.id_store] = store;
			return store.id_store;
		},
		async updateStore(id, params) {
			this.stores[id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteStore(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${id}`,
				useToken: "access",
			});
			delete this.stores[id];
		},
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

		async getBoxByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxs[idStore] || clear) {
				this.boxs[idStore] = {};
			}
			this.boxsLoading = true;
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newBoxList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box?${paramString}`,
				useToken: "access",
			});
			for (const box of newBoxList["data"]) {
				hydrateBox(this, idStore, box.id_box, box, expand);
			}
			this.boxsTotalCount[idStore] = newBoxList["pagination"]?.["total"] || 0;
			this.boxsLoading = false;
			return [newBoxList["pagination"]?.["nextOffset"] || 0, newBoxList["pagination"]?.["hasMore"] || false];
		},
		async getBoxById(idStore, id, expand = []) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			if (!this.boxs[idStore][id]) {
				this.boxs[idStore][id] = {};
			}
			this.boxs[idStore][id].loading = true;
			const paramString = buildQuery({ expand });
			let box = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${id}?${paramString}`,
				useToken: "access",
			});
			hydrateBox(this, idStore, box.id_box, box, expand);
		},
		async createBox(idStore, params) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			const box = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box`,
				useToken: "access",
				body: params,
			});
			this.boxs[idStore][box.id_box] = box;
		},
		async updateBox(idStore, id, params) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			this.boxs[idStore][id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteBox(idStore, id) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${id}`,
				useToken: "access",
			});
			delete this.boxs[idStore][id];
		},
		async createBoxBulk(idStore, params) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			const boxBulk = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const box of boxBulk["valide"]) {
				this.boxs[idStore][box.id_box] = box;
			}
		},
		async updateBoxBulk(idStore, params) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			const boxBulk = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const box of boxBulk["valide"]) {
				this.boxs[idStore][box.id_box] = box;
			}
		},
		async deleteBoxBulk(idStore, params) {
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			const boxBulk = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const box of boxBulk["valide"]) {
				delete this.boxs[idStore][box.id_box];
			}
		},
		async showBoxById(idStore, id, params) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		async getLedByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.leds[idStore] || clear) {
				this.leds[idStore] = {};
			}
			this.ledsLoading = true;
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newLedList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/led?${paramString}`,
				useToken: "access",
			});
			for (const led of newLedList["data"]) {
				this.leds[idStore][led.id_led] = led;
			}
			this.ledsTotalCount[idStore] = newLedList["pagination"]?.["total"] || 0;
			this.ledsLoading = false;
			return [newLedList["pagination"]?.["nextOffset"] || 0, newLedList["pagination"]?.["hasMore"] || false];
		},
		async getLedById(idStore, id) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			if (!this.leds[idStore][id]) {
				this.leds[idStore][id] = {};
			}
			this.leds[idStore][id].loading = true;
			this.leds[idStore][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
			});
		},
		async createLed(idStore, params) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			const led = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led`,
				useToken: "access",
				body: params,
			});
			this.leds[idStore][led.id_led] = led;
		},
		async updateLed(idStore, id, params) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			this.leds[idStore][id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteLed(idStore, id) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
			});
			delete this.leds[idStore][id];
		},
		async createLedBulk(idStore, params) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			const ledBulk = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			for (const led of ledBulk["valide"]) {
				this.leds[idStore][led.id_led] = led;
			}
		},
		async updateLedBulk(idStore, params) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			const ledBulk = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			for (const led of ledBulk["valide"]) {
				this.leds[idStore][led.id_led] = led;
			}
		},
		async deleteLedBulk(idStore, params) {
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			const ledBulk = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			for (const led of ledBulk["valide"]) {
				delete this.leds[idStore][led.id_led];
			}
		},
		async showLedById(idStore, id, params) {
			await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/${id}/show`,
				useToken: "access",
				body: params,
			});
		},

		async getTagStoreByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.storeTags[idStore] || clear) {
				this.storeTags[idStore] = {};
			}
			const tagsStore = useTagsStore();
			this.storeTagsLoading = true;
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newTagList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.storeTags[idStore][tag.id_tag] = tag;
				if (expand.includes("tag")) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.storeTagsTotalCount[idStore] = newTagList["pagination"]?.["total"] || 0;
			this.storeTagsLoading = false;
			return [newTagList["pagination"]?.["nextOffset"] || 0, newTagList["pagination"]?.["hasMore"] || false];
		},
		async getTagStoreById(idStore, id, expand = []) {
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			if (!this.storeTags[idStore][id]) {
				this.storeTags[idStore][id] = {};
			}
			const tagsStore = useTagsStore();
			this.storeTags[idStore][id].loading = true;
			const paramString = buildQuery({ expand });
			this.storeTags[idStore][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/tag/${id}?${paramString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[id] = this.storeTags[idStore][id].tag;
			}
		},
		async createTagStore(idStore, params) {
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			const tagStore = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/tag`,
				useToken: "access",
				body: params,
			});
			this.storeTags[idStore][tagStore.id_tag] = tagStore;
		},
		async deleteTagStore(idStore, id) {
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/tag/${id}`,
				useToken: "access",
			});
			delete this.storeTags[idStore][id];
		},
		async createTagStoreBulk(idStore, params) {
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			const tagStoreBulk = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of tagStoreBulk["valide"]) {
				this.storeTags[idStore][tag.id_tag] = tag;
			}
		},
		async deleteTagStoreBulk(idStore, params) {
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			const tagStoreBulk = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of tagStoreBulk["valide"]) {
				delete this.storeTags[idStore][tag.id_tag];
			}
		},

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
