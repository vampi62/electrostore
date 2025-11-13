import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useTagsStore, useItemsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useStoresStore = defineStore("stores",{
	state: () => ({
		storesLoading: true,
		storesTotalCount: 0,
		stores: {},
		storeEdition: {},

		boxsLoading: true,
		boxsTotalCount: {},
		boxs: {},
		boxEdition: {},

		ledsLoading: true,
		ledsTotalCount: {},
		leds: {},
		ledEdition: {},

		storeTagsLoading: true,
		storeTagsTotalCount: {},
		storeTags: {},
		storeTagEdition: {},

		boxItemsLoading: true,
		boxItemsTotalCount: {},
		boxItems: {},
		boxItemEdition: {},

		boxTagsLoading: true,
		boxTagsTotalCount: {},
		boxTags: {},
		boxTagEdition: {},
	}),
	actions: {
		async getStoreByList(idResearch = [], expand = []) {
			this.storesLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?&idResearch=${idResearch}&${expandString}`,
				useToken: "access",
			});
			for (const store of newStoreList["data"]) {
				this.stores[store.id_store] = store;
				this.boxsTotalCount[store.id_store] = store.boxs_count;
				this.ledsTotalCount[store.id_store] = store.leds_count;
				this.storeTagsTotalCount[store.id_store] = store.stores_tags_count;
				if (expand.includes("boxs")) {
					this.boxs[store.id_store] = {};
					for (const box of store.boxs) {
						this.boxs[store.id_store][box.id_box] = box;
					}
				}
				if (expand.includes("leds")) {
					this.leds[store.id_store] = {};
					for (const led of store.leds) {
						this.leds[store.id_store][led.id_led] = led;
					}
				}
				if (expand.includes("stores_tags")) {
					this.storeTags[store.id_store] = {};
					for (const tag of store.stores_tags) {
						this.storeTags[store.id_store][tag.id_tag] = tag;
					}
				}
			}
			this.storesTotalCount = newStoreList["count"];
			this.storesLoading = false;
		},
		async getStoreByInterval(limit = 100, offset = 0, expand = []) {
			this.storesLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const store of newStoreList["data"]) {
				this.stores[store.id_store] = store;
				this.boxsTotalCount[store.id_store] = store.boxs_count;
				this.ledsTotalCount[store.id_store] = store.leds_count;
				this.storeTagsTotalCount[store.id_store] = store.stores_tags_count;
				if (expand.includes("boxs")) {
					this.boxs[store.id_store] = {};
					for (const box of store.boxs) {
						this.boxs[store.id_store][box.id_box] = box;
					}
				}
				if (expand.includes("leds")) {
					this.leds[store.id_store] = {};
					for (const led of store.leds) {
						this.leds[store.id_store][led.id_led] = led;
					}
				}
				if (expand.includes("stores_tags")) {
					this.storeTags[store.id_store] = {};
					for (const tag of store.stores_tags) {
						this.storeTags[store.id_store][tag.id_tag] = tag;
					}
				}
			}
			this.storesTotalCount = newStoreList["count"];
			this.storesLoading = false;
		},
		async getStoreById(id, expand = []) {
			if (!this.stores[id]) {
				this.stores[id] = {};
			}
			this.stores[id] = { ...this.stores[id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.stores[id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${id}?${expandString}`,
				useToken: "access",
			});
			this.boxsTotalCount[id] = this.stores[id].boxs_count;
			this.ledsTotalCount[id] = this.stores[id].leds_count;
			this.storeTagsTotalCount[id] = this.stores[id].stores_tags_count;
			if (expand.includes("boxs")) {
				this.boxs[id] = {};
				for (const box of this.stores[id].boxs) {
					this.boxs[id][box.id_box] = box;
				}
			}
			if (expand.includes("leds")) {
				this.leds[id] = {};
				for (const led of this.stores[id].leds) {
					this.leds[id][led.id_led] = led;
				}
			}
			if (expand.includes("stores_tags")) {
				this.storeTags[id] = {};
				for (const tag of this.stores[id].stores_tags) {
					this.storeTags[id][tag.id_tag] = tag;
				}
			}
		},
		async createStore(id, params) {
			this.storeEdition[id].loading = true;
			this.storeEdition[id] = await fetchWrapper.post({
				url: `${baseUrl}/store`,
				useToken: "access",
				body: params,
			});
			this.stores[this.storeEdition[id].id_store] = this.storeEdition[id];
		},
		async updateStore(id, params) {
			this.storeEdition[id].loading = true;
			this.storeEdition[id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${id}`,
				useToken: "access",
				body: params,
			});
			this.stores[id] = this.storeEdition[id];
		},
		async deleteStore(id) {
			this.storeEdition[id].loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${id}`,
				useToken: "access",
			});
			delete this.stores[id];
			this.storeEdition[id] = {};
		},
		async createStoreComplete(id, params) {
			this.storeEdition[id].loading = true;
			this.storeEdition[id] = await fetchWrapper.post({
				url: `${baseUrl}/store/complete`,
				useToken: "access",
				body: params,
			});
			this.stores[this.storeEdition[id].store.id_store] = this.storeEdition[id];
		},
		async updateStoreComplete(id, params) {
			this.storeEdition[id].loading = true;
			this.storeEdition[id] = await fetchWrapper.put({
				url: `${baseUrl}/store/${id}/complete`,
				useToken: "access",
				body: params,
			});
			this.stores[id] = this.storeEdition[id];
		},

		async getBoxByInterval(idStore, limit = 100, offset = 0, expand = []) {
			// init list if not exist
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			this.boxsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newBoxList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const box of newBoxList["data"]) {
				this.boxs[idStore][box.id_box] = box;
				this.boxItemsTotalCount[box.id_box] = box.item_boxs_count;
				this.boxTagsTotalCount[box.id_box] = box.box_tags_count;
				if (expand.includes("item_boxs")) {
					this.boxItems[box.id_box] = {};
					for (const item of box.item_boxs) {
						this.boxItems[box.id_box][item.id_item] = item;
					}
				}
				if (expand.includes("box_tags")) {
					this.boxTags[box.id_box] = {};
					for (const tag of box.box_tags) {
						this.boxTags[box.id_box][tag.id_tag] = tag;
					}
				}
			}
			this.boxsTotalCount = newBoxList["count"];
			this.boxsLoading = false;
		},
		async getBoxById(idStore, id, expand = []) {
			// init list if not exist
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			if (!this.boxs[idStore][id]) {
				this.boxs[idStore][id] = {};
			}
			this.boxs[idStore][id] = { ...this.boxs[idStore][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.boxs[idStore][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${id}?${expandString}`,
				useToken: "access",
			});
			this.boxItemsTotalCount[id] = this.boxs[idStore][id].item_boxs_count;
			this.boxTagsTotalCount[id] = this.boxs[idStore][id].box_tags_count;
			if (expand.includes("item_boxs")) {
				this.boxItems[id] = {};
				for (const item of this.boxs[idStore][id].item_boxs) {
					this.boxItems[id][item.id_item] = item;
				}
			}
			if (expand.includes("box_tags")) {
				this.boxTags[id] = {};
				for (const tag of this.boxs[idStore][id].box_tags) {
					this.boxTags[id][tag.id_tag] = tag;
				}
			}
		},
		async createBox(idStore, params) {
			this.boxEdition.loading = true;
			this.boxEdition = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box`,
				useToken: "access",
				body: params,
			});
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			this.boxs[idStore][this.boxEdition.id_box] = this.boxEdition;
		},
		async updateBox(idStore, id, params) {
			this.boxEdition.loading = true;
			this.boxEdition = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/${id}`,
				useToken: "access",
				body: params,
			});
			this.boxs[idStore][id] = this.boxEdition;
		},
		async deleteBox(idStore, id) {
			this.boxEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${id}`,
				useToken: "access",
			});
			delete this.boxs[idStore][id];
			this.boxEdition = {};
		},
		async createBoxBulk(idStore, params) {
			let boxList = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			for (const box of boxList["valide"]) {
				this.boxs[idStore][box.id_box] = box;
			}
		},
		async updateBoxBulk(idStore, params) {
			let boxList = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.boxs[idStore]) {
				this.boxs[idStore] = {};
			}
			for (const box of boxList["valide"]) {
				this.boxs[idStore][box.id_box] = box;
			}
		},
		async deleteBoxBulk(idStore, params) {
			let boxList = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const box of boxList["valide"]) {
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

		async getLedByInterval(idStore, limit = 100, offset = 0) {
			// init list if not exist
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			this.ledsLoading = true;
			let newLedList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/led?limit=${limit}&offset=${offset}`,
				useToken: "access",
			});
			for (const led of newLedList["data"]) {
				this.leds[idStore][led.id_led] = led;
			}
			this.ledsTotalCount = newLedList["count"];
			this.ledsLoading = false;
		},
		async getLedById(idStore, id) {
			// init list if not exist
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			if (!this.leds[idStore][id]) {
				this.leds[idStore][id] = {};
			}
			this.leds[idStore][id] = { ...this.leds[idStore][id], loading: true };
			this.leds[idStore][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
			});
		},
		async createLed(idStore, params) {
			this.ledEdition.loading = true;
			this.ledEdition = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led`,
				useToken: "access",
				body: params,
			});
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			this.leds[idStore][this.ledEdition.id_led] = this.ledEdition;
		},
		async updateLed(idStore, id, params) {
			this.ledEdition.loading = true;
			this.ledEdition = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
				body: params,
			});
			this.leds[idStore][id] = this.ledEdition;
		},
		async deleteLed(idStore, id) {
			this.ledEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/led/${id}`,
				useToken: "access",
			});
			delete this.leds[idStore][id];
			this.ledEdition = {};
		},
		async createLedBulk(idStore, params) {
			let ledList = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			for (const led of ledList["valide"]) {
				this.leds[idStore][led.id_led] = led;
			}
		},
		async updateLedBulk(idStore, params) {
			let ledList = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.leds[idStore]) {
				this.leds[idStore] = {};
			}
			for (const led of ledList["valide"]) {
				this.leds[idStore][led.id_led] = led;
			}
		},
		async deleteLedBulk(idStore, params) {
			let ledList = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/led/bulk`,
				useToken: "access",
				body: params,
			});
			for (const led of ledList["valide"]) {
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

		async getTagStoreByInterval(idStore, limit = 100, offset = 0, expand = []) {
			// init list if not exist
			const tagsStore = useTagsStore();
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			this.storeTagsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newTagList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.storeTags[idStore][tag.id_tag] = tag;
				if (expand.includes("tag")) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.storeTagsTotalCount[idStore] = newTagList["count"];
			this.storeTagsLoading = false;
		},
		async getTagStoreById(idStore, id, expand = []) {
			// init list if not exist
			const tagsStore = useTagsStore();
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			if (!this.storeTags[idStore][id]) {
				this.storeTags[idStore][id] = {};
			}
			this.storeTags[idStore][id] = { ...this.storeTags[idStore][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.storeTags[idStore][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[id] = this.storeTags[idStore][id].tag;
			}
		},
		async createTagStore(idStore, params) {
			this.storeTagEdition.loading = true;
			this.storeTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/tag`,
				useToken: "access",
				body: params,
			});
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			this.storeTags[idStore][this.storeTagEdition.id_tag] = this.storeTagEdition;
		},
		async deleteTagStore(idStore, id) {
			this.storeTagEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/tag/${id}`,
				useToken: "access",
			});
			delete this.storeTags[idStore][id];
			this.storeTagEdition = {};
		},
		async createTagStoreBulk(idStore, params) {
			let tagStoreList = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.storeTags[idStore]) {
				this.storeTags[idStore] = {};
			}
			for (const tag of tagStoreList["valide"]) {
				this.storeTags[idStore][tag.id_tag] = tag;
			}
		},
		async deleteTagStoreBulk(idStore, params) {
			let tagStoreList = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of tagStoreList["valide"]) {
				delete this.storeTags[idStore][tag.id_tag];
			}
		},

		async getBoxItemByList(idStore, idBox, idResearch = [], expand = []) {
			// init list if not exist
			const itemsStore = useItemsStore();
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			this.boxItemsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.boxItems[idBox][item.id_item] = item;
				if (expand.includes("item")) {
					itemsStore.items[item.id_item] = item.item;
				}
			}
			this.boxItemsTotalCount[idBox] = newItemList["count"];
			this.boxItemsLoading = false;
		},
		async getBoxItemByInterval(idStore, idBox, limit = 100, offset = 0, expand = []) {
			// init list if not exist
			const itemsStore = useItemsStore();
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			this.boxItemsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.boxItems[idBox][item.id_item] = item;
				if (expand.includes("item")) {
					itemsStore.items[item.id_item] = item.item;
				}
			}
			this.boxItemsTotalCount = newItemList["count"];
			this.boxItemsLoading = false;
		},
		async getBoxItemById(idStore, idBox, id, expand = []) {
			// init list if not exist
			const itemsStore = useItemsStore();
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			if (!this.boxItems[idBox][id]) {
				this.boxItems[idBox][id] = {};
			}
			this.boxItems[idBox][id] = { ...this.boxItems[idBox][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.boxItems[idBox][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("item")) {
				itemsStore.items[id] = this.boxItems[idBox][id].item;
			}
		},
		async createBoxItem(idStore, idBox, params) {
			this.boxItemEdition.loading = true;
			this.boxItemEdition = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item`,
				useToken: "access",
				body: params,
			});
			if (!this.boxItems[idBox]) {
				this.boxItems[idBox] = {};
			}
			this.boxItems[idBox][this.boxItemEdition.id_item] = this.boxItemEdition;
		},
		async updateBoxItem(idStore, idBox, id, params) {
			this.boxItemEdition.loading = true;
			this.boxItemEdition = await fetchWrapper.put({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
				body: params,
			});
			this.boxItems[idBox][id] = this.boxItemEdition;
		},
		async deleteBoxItem(idStore, idBox, id) {
			this.boxItemEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/item/${id}`,
				useToken: "access",
			});
			delete this.boxItems[idBox][id];
			this.boxItemEdition = {};
		},

		async getBoxTagByInterval(idStore, idBox, limit = 100, offset = 0, expand = []) {
			// init list if not exist
			const tagsStore = useTagsStore();
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			this.boxTagsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newTagList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
				if (expand.includes("tag")) {
					tagsStore.tags[tag.id_tag] = tag.tag;
				}
			}
			this.boxTagsTotalCount = newTagList["count"];
			this.boxTagsLoading = false;
		},
		async getBoxTagById(idStore, idBox, id, expand = []) {
			// init list if not exist
			const tagsStore = useTagsStore();
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			if (!this.boxTags[idBox][id]) {
				this.boxTags[idBox][id] = {};
			}
			this.boxTags[idBox][id] = { ...this.boxTags[idBox][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.boxTags[idBox][id] = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[id] = this.boxTags[idBox][id].tag;
			}
		},
		async createBoxTag(idStore, idBox, params) {
			this.boxTagEdition.loading = true;
			this.boxTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag`,
				useToken: "access",
				body: params,
			});
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			this.boxTags[idBox][this.boxTagEdition.id_tag] = this.boxTagEdition;
		},
		async deleteBoxTag(idStore, idBox, id) {
			this.boxTagEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/${id}`,
				useToken: "access",
			});
			delete this.boxTags[idBox][id];
			this.boxTagEdition = {};
		},
		async createBoxTagBulk(idStore, idBox, params) {
			let boxTagList = await fetchWrapper.post({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.boxTags[idBox]) {
				this.boxTags[idBox] = {};
			}
			for (const tag of boxTagList["valide"]) {
				this.boxTags[idBox][tag.id_tag] = tag;
			}
		},
		async deleteBoxTagBulk(idStore, idBox, params) {
			let boxTagList = await fetchWrapper.delete({
				url: `${baseUrl}/store/${idStore}/box/${idBox}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of boxTagList["valide"]) {
				delete this.boxTags[idBox][tag.id_tag];
			}
		},
	},
});
