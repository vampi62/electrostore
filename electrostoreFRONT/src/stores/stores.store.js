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
		storeReady: {},

		boxsLoading: true,
		boxsTotalCount: {},
		boxs: {},
		boxEdition: {},
		boxReady: {},

		ledsLoading: true,
		ledsTotalCount: {},
		leds: {},
		ledEdition: {},
		ledReady: {},

		storeTagsLoading: true,
		storeTagsTotalCount: {},
		storeTags: {},
		storeTagEdition: {},
		storeTagReady: {},

		boxItemsLoading: true,
		boxItemsTotalCount: {},
		boxItems: {},
		boxItemEdition: {},
		boxItemReady: {},

		boxTagsLoading: true,
		boxTagsTotalCount: {},
		boxTags: {},
		boxTagEdition: {},
		boxTagReady: {},
	}),
	actions: {
		async getStoreByList(idResearch = [], expand = []) {
			this.storesLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?${paramString}`,
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
			this.storesLoading = false;
		},
		async getStoreByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.storesLoading = true;
			if (clear) {
				this.stores = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newStoreList = await fetchWrapper.get({
				url: `${baseUrl}/store?${paramString}`,
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
			this.storesTotalCount = newStoreList["pagination"]?.["total"] || 0;
			this.storesLoading = false;
			return [newStoreList["pagination"]?.["nextOffset"] || 0, newStoreList["pagination"]?.["hasMore"] || false];
		},
		async getStoreById(id, expand = []) {
			if (!this.stores[id]) {
				this.stores[id] = {};
			}
			this.stores[id].loading = true;
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
		async pushStoreReady() {
			for (const id in this.storeReady) {
				const change = this.storeReady[id];
				if (change.status === "new") {
					const newId = await this.createStore(change.data);
					this.updateIdStore(id, newId);
					this.pushBoxReady(newId);
					this.pushLedReady(newId);
					this.pushTagStoreReady(newId);
					delete this.storeReady[id];
				} else if (change.status === "modified") {
					await this.updateStore(id, change.data);
					this.pushBoxReady(id);
					this.pushLedReady(id);
					this.pushTagStoreReady(id);
					delete this.storeReady[id];
				} else if (change.status === "delete") {
					await this.deleteStore(id);
					delete this.storeReady[id];
				}
			}
		},
		async pushStoreById(id) {
			if (!this.storeEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createStore(this.storeEdition[id]);
				this.updateIdStore(id, newId);
			} else {
				await this.updateStore(id, this.storeEdition[id]);
			}
			this.pushBoxReady(newId);
			this.pushLedReady(newId);
			this.pushTagStoreReady(newId);
			return newId;
		},
		updateIdStore(oldId, newId) {
			if (this.boxReady[oldId]) {
				this.boxReady[newId] = { ...this.boxReady[oldId], id_store: newId };
				delete this.boxReady[oldId];
			}
			if (this.boxEdition[oldId]) {
				this.boxEdition[newId] = { ...this.boxEdition[oldId], id_store: newId };
				delete this.boxEdition[oldId];
			}
			if (this.ledReady[oldId]) {
				this.ledReady[newId] = { ...this.ledReady[oldId], id_store: newId };
				delete this.ledReady[oldId];
			}
			if (this.ledEdition[oldId]) {
				this.ledEdition[newId] = { ...this.ledEdition[oldId], id_store: newId };
				delete this.ledEdition[oldId];
			}
		},
		commitStoreEdition(id, operation = "modified") { // return (sucess:bool, newStatus:string)
			if (!this.storeEdition[id]) {
				return { success: false, newStatus: null };
			}
			if (!this.storeReady[id]) {
				this.storeReady[id] = {};
			}
			if (this.storeReady[id].status === "new" && operation === "delete") {
				delete this.storeReady[id];
				return { success: true, newStatus: "delete" };
			} else if (this.storeReady[id].status === "modified" && operation === "delete") {
				this.storeReady[id].status = "delete";
			} else if (this.storeReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.storeReady[id].status = "modified";
			} else {
				this.storeReady[id].status = operation;
			}
			this.storeReady[id].data = { ...this.storeEdition[id] };
			return { success: true, newStatus: this.storeReady[id].status };
		},
		getAvailableEditionStore() {
			// search existing "new{id}" in storeEdition to find available id for new STORE
			const newIds = Math.max(Object.keys(this.storeEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearStoreEdition() {
			this.storeEdition = {};
			this.storeReady = {};
		},
		clearStoreEditionById(id) {
			delete this.storeEdition[id];
			delete this.storeReady[id];
			this.clearBoxEdition(id);
			this.clearLedEdition(id);
			this.clearTagStoreEdition(id);
		},

		async getBoxByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxs[idStore] || clear) {
				this.boxs[idStore] = {};
			}
			this.boxsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newBoxList = await fetchWrapper.get({
				url: `${baseUrl}/store/${idStore}/box?${paramString}`,
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
		commitBoxEdition(idStore, id, operation = "modified") {
			if (!this.boxEdition[idStore] || !this.boxEdition[idStore][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.boxReady[idStore]) {
				this.boxReady[idStore] = {};
			}
			if (!this.boxReady[idStore][id]) {
				this.boxReady[idStore][id] = {};
			}
			if (this.boxReady[idStore][id].status === "new" && operation === "delete") {
				delete this.boxReady[idStore][id];
				return { success: true, newStatus: "delete" };
			} else if (this.boxReady[idStore][id].status === "modified" && operation === "delete") {
				this.boxReady[idStore][id].status = "delete";
			} else if (this.boxReady[idStore][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.boxReady[idStore][id].status = "modified";
			} else {
				this.boxReady[idStore][id].status = operation;
			}
			this.boxReady[idStore][id].data = { ...this.boxEdition[idStore][id] };
			return { success: true, newStatus: this.boxReady[id].status };
		},
		async pushBoxReady(idStore) {
			if (!this.boxReady[idStore]) {
				return;
			}
			for (const id in this.boxReady[idStore]) {
				const change = this.boxReady[idStore][id];
				if (change.status === "new") {
					await this.createBox(idStore, change.data);
					delete this.boxReady[id];
				} else if (change.status === "modified") {
					await this.updateBox(idStore, id, change.data);
					delete this.boxReady[id];
				} else if (change.status === "delete") {
					await this.deleteBox(idStore, id);
					delete this.boxReady[id];
				}
			}
		},
		clearBoxEdition(idStore) {
			this.boxEdition[idStore] = {};
			this.boxReady[idStore] = {};
		},

		async getLedByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.leds[idStore] || clear) {
				this.leds[idStore] = {};
			}
			this.ledsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
		commitLedEdition(idStore, id, operation = "modified") {
			if (!this.ledEdition[idStore] || !this.ledEdition[idStore][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.ledReady[idStore]) {
				this.ledReady[idStore] = {};
			}
			if (!this.ledReady[idStore][id]) {
				this.ledReady[idStore][id] = {};
			}
			if (this.ledReady[idStore][id].status === "new" && operation === "delete") {
				delete this.ledReady[idStore][id];
				return { success: true, newStatus: "delete" };
			} else if (this.ledReady[idStore][id].status === "modified" && operation === "delete") {
				this.ledReady[idStore][id].status = "delete";
			} else if (this.ledReady[idStore][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.ledReady[idStore][id].status = "modified";
			} else {
				this.ledReady[idStore][id].status = operation;
			}
			this.ledReady[idStore][id].data = { ...this.ledEdition[idStore][id] };
			return { success: true, newStatus: this.ledReady[id].status };
		},
		async pushLedReady(idStore) {
			if (!this.ledReady[idStore]) {
				return;
			}
			for (const id in this.ledReady[idStore]) {
				const change = this.ledReady[idStore][id];
				if (change.status === "new") {
					await this.createLed(idStore, change.data);
					delete this.ledReady[id];
				} else if (change.status === "modified") {
					await this.updateLed(idStore, id, change.data);
					delete this.ledReady[id];
				} else if (change.status === "delete") {
					await this.deleteLed(idStore, id);
					delete this.ledReady[id];
				}
			}
		},
		clearLedEdition(idStore) {
			this.ledEdition[idStore] = {};
			this.ledReady[idStore] = {};
		},

		async getTagStoreByInterval(idStore, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.storeTags[idStore] || clear) {
				this.storeTags[idStore] = {};
			}
			const tagsStore = useTagsStore();
			this.storeTagsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
		commitTagStoreEdition(idStore, id, operation = "modified") {
			if (!this.tagStoreEdition[idStore] || !this.tagStoreEdition[idStore][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.tagStoreReady[idStore]) {
				this.tagStoreReady[idStore] = {};
			}
			if (!this.tagStoreReady[idStore][id]) {
				this.tagStoreReady[idStore][id] = {};
			}
			if (this.tagStoreReady[idStore][id].status === "new" && operation === "delete") {
				delete this.tagStoreReady[idStore][id];
				return { success: true, newStatus: "delete" };
			} else if (this.tagStoreReady[idStore][id].status === "modified" && operation === "delete") {
				this.tagStoreReady[idStore][id].status = "delete";
			} else if (this.tagStoreReady[idStore][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.tagStoreReady[idStore][id].status = "modified";
			} else {
				this.tagStoreReady[idStore][id].status = operation;
			}
			this.tagStoreReady[idStore][id].data = { ...this.tagStoreEdition[idStore][id] };
			return { success: true, newStatus: this.tagStoreReady[id].status };
		},
		async pushTagStoreReady(idStore) {
			if (!this.tagStoreReady[idStore]) {
				return;
			}
			for (const id in this.tagStoreReady[idStore]) {
				const change = this.tagStoreReady[idStore][id];
				if (change.status === "new") {
					await this.createTagStore(idStore, change.data);
					delete this.tagStoreReady[id];
				} else if (change.status === "modified") {
					await this.updateTagStore(idStore, id, change.data);
					delete this.tagStoreReady[id];
				} else if (change.status === "delete") {
					await this.deleteTagStore(idStore, id);
					delete this.tagStoreReady[id];
				}
			}
		},
		clearTagStoreEdition(idStore) {
			this.tagStoreEdition[idStore] = {};
			this.tagStoreReady[idStore] = {};
		},

		async getBoxItemByInterval(idStore, idBox, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxItems[idBox] || clear) {
				this.boxItems[idBox] = {};
			}
			this.boxItemsLoading = true;
			const itemsStore = useItemsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
		commitBoxItemEdition(idBox, id, operation = "modified") {
		},
		async pushBoxItemReady(idBox) {
		},
		clearBoxItemEdition(idBox) {
		},

		async getBoxTagByInterval(idStore, idBox, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.boxTags[idBox] || clear) {
				this.boxTags[idBox] = {};
			}
			this.boxTagsLoading = true;
			const tagsStore = useTagsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
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
		commitBoxTagEdition(idBox, id, operation = "modified") {
		},
		async pushBoxTagReady(idBox) {
		},
		clearBoxTagEdition(idBox) {
		},
	},
});
