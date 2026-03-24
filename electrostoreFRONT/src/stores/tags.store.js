import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useStoresStore, useItemsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useTagsStore = defineStore("tags",{
	state: () => ({
		tagsLoading: true,
		tagsTotalCount: 0,
		tags: {},
		tagEdition: {},
		tagReady: {},

		tagsStoreLoading: true,
		tagsStoreTotalCount: {},
		tagsStore: {},
		tagStoreEdition: {},
		tagStoreReady: {},

		tagsBoxLoading: true,
		tagsBoxTotalCount: {},
		tagsBox: {},
		tagBoxEdition: {},
		tagBoxReady: {},

		tagsItemLoading: true,
		tagsItemTotalCount: {},
		tagsItem: {},
		tagItemEdition: {},
		tagItemReady: {},
	}),
	actions: {
		async getTagByList(idResearch = [], expand = []) {
			this.tagsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newTagList = await fetchWrapper.get({
				url: `${baseUrl}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.tags[tag.id_tag] = tag;
				this.tagsStoreTotalCount[tag.id_tag] = tag.stores_tags_count;
				this.tagsBoxTotalCount[tag.id_tag] = tag.boxs_tags_count;
				this.tagsItemTotalCount[tag.id_tag] = tag.items_tags_count;
				if (expand.includes("stores_tags")) {
					this.tagsStore[tag.id_tag] = {};
					for (const tagStore of tag.stores_tags) {
						this.tagsStore[tag.id_tag][tagStore.id_store] = tagStore;
					}
				}
				if (expand.includes("boxs_tags")) {
					this.tagsBox[tag.id_tag] = {};
					for (const tagBox of tag.boxs_tags) {
						this.tagsBox[tag.id_tag][tagBox.id_box] = tagBox;
					}
				}
				if (expand.includes("items_tags")) {
					this.tagsItem[tag.id_tag] = {};
					for (const tagItem of tag.items_tags) {
						this.tagsItem[tag.id_tag][tagItem.id_item] = tagItem;
					}
				}
			}
			this.tagsLoading = false;
		},
		async getTagByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.tagsLoading = true;
			if (clear) {
				this.tags = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newTagList = await fetchWrapper.get({
				url: `${baseUrl}/tag?${paramString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.tags[tag.id_tag] = tag;
				this.tagsStoreTotalCount[tag.id_tag] = tag.stores_tags_count;
				this.tagsBoxTotalCount[tag.id_tag] = tag.boxs_tags_count;
				this.tagsItemTotalCount[tag.id_tag] = tag.items_tags_count;
				if (expand.includes("stores_tags")) {
					this.tagsStore[tag.id_tag] = {};
					for (const tagStore of tag.stores_tags) {
						this.tagsStore[tag.id_tag][tagStore.id_store] = tagStore;
					}
				}
				if (expand.includes("boxs_tags")) {
					this.tagsBox[tag.id_tag] = {};
					for (const tagBox of tag.boxs_tags) {
						this.tagsBox[tag.id_tag][tagBox.id_box] = tagBox;
					}
				}
				if (expand.includes("items_tags")) {
					this.tagsItem[tag.id_tag] = {};
					for (const tagItem of tag.items_tags) {
						this.tagsItem[tag.id_tag][tagItem.id_item] = tagItem;
					}
				}
			}
			this.tagsTotalCount = newTagList["pagination"]?.["total"] || 0;
			this.tagsLoading = false;
			return [newTagList["pagination"]?.["nextOffset"] || 0, newTagList["pagination"]?.["hasMore"] || false];
		},
		async getTagById(id, expand = []) {
			if (!this.tags[id]) {
				this.tags[id] = {};
			}
			this.tags[id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tags[id] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			this.tagsStoreTotalCount[id] = this.tags[id].stores_tags_count;
			this.tagsBoxTotalCount[id] = this.tags[id].boxs_tags_count;
			this.tagsItemTotalCount[id] = this.tags[id].items_tags_count;
			if (expand.includes("stores_tags")) {
				this.tagsStore[id] = {};
				for (const tagStore of this.tags[id].stores_tags) {
					this.tagsStore[id][tagStore.id_store] = tagStore;
				}
			}
			if (expand.includes("boxs_tags")) {
				this.tagsBox[id] = {};
				for (const tagBox of this.tags[id].boxs_tags) {
					this.tagsBox[id][tagBox.id_box] = tagBox;
				}
			}
			if (expand.includes("items_tags")) {
				this.tagsItem[id] = {};
				for (const tagItem of this.tags[id].items_tags) {
					this.tagsItem[id][tagItem.id_item] = tagItem;
				}
			}
		},
		async createTag(params) {
			const tag = await fetchWrapper.post({
				url: `${baseUrl}/tag`,
				useToken: "access",
				body: params,
			});
			this.tags[tag.id_tag] = tag;
			return tag.id_tag;
		},
		async updateTag(id, params) {
			if (params?.nom_tag && params?.nom_tag === this.tags[id]?.nom_tag) {
				delete params.nom_tag;
			}
			this.tags[id] = await fetchWrapper.put({
				url: `${baseUrl}/tag/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteTag(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${id}`,
				useToken: "access",
			});
			delete this.tags[id];
		},
		async createTagBulk(params) {
			const tagBulk = await fetchWrapper.post({
				url: `${baseUrl}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of tagBulk["valide"]) {
				this.tags[tag.id_tag] = tag;
			}
		},
		async pushTagReady() {
			for (const id in this.tagReady) {
				const change = this.tagReady[id];
				if (change.status === "new") {
					const newId = await this.createTag(change.data);
					this.updateIdTag(id, newId);
					this.pushTagStoreReady(newId);
					this.pushTagBoxReady(newId);
					this.pushTagItemReady(newId);
					delete this.tagReady[id];
				} else if (change.status === "modified") {
					await this.updateTag(id, change.data);
					this.pushTagStoreReady(id);
					this.pushTagBoxReady(id);
					this.pushTagItemReady(id);
					delete this.tagReady[id];
				} else if (change.status === "delete") {
					await this.deleteTag(id);
					delete this.tagReady[id];
				}
			}
		},
		async pushTagById(id) {
			if (!this.tagEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createTag(this.tagEdition[id]);
				this.updateIdTag(id, newId);
			} else {
				await this.updateTag(id, this.tagEdition[id]);
			}
			this.pushTagStoreReady(newId);
			this.pushTagBoxReady(newId);
			this.pushTagItemReady(newId);
			return newId;
		},
		updateIdTag(oldId, newId) {
			if (this.tagStoreReady[oldId]) {
				this.tagStoreReady[newId] = { ...this.tagStoreReady[oldId], id_tag: newId };
				delete this.tagStoreReady[oldId];
			}
			if (this.tagStoreEdition[oldId]) {
				this.tagStoreEdition[newId] = { ...this.tagStoreEdition[oldId], id_tag: newId };
				delete this.tagStoreEdition[oldId];
			}
			if (this.tagBoxReady[oldId]) {
				this.tagBoxReady[newId] = { ...this.tagBoxReady[oldId], id_tag: newId };
				delete this.tagBoxReady[oldId];
			}
			if (this.tagBoxEdition[oldId]) {
				this.tagBoxEdition[newId] = { ...this.tagBoxEdition[oldId], id_tag: newId };
				delete this.tagBoxEdition[oldId];
			}
			if (this.tagItemReady[oldId]) {
				this.tagItemReady[newId] = { ...this.tagItemReady[oldId], id_tag: newId };
				delete this.tagItemReady[oldId];
			}
			if (this.tagItemEdition[oldId]) {
				this.tagItemEdition[newId] = { ...this.tagItemEdition[oldId], id_tag: newId };
				delete this.tagItemEdition[oldId];
			}
		},
		commitTagEdition(id, operation = "modified") { // return (sucess:bool, newStatus:string)
			if (!this.tagEdition[id]) {
				return { success: false, newStatus: null };
			}
			if (!this.tagReady[id]) {
				this.tagReady[id] = {};
				this.tagReady[id].data = { id_tag: id };
			}
			if (this.tagReady[id].status === "new" && operation === "delete") {
				delete this.tagReady[id];
				return { success: true, newStatus: "delete" };
			} else if (this.tagReady[id].status === "modified" && operation === "delete") {
				this.tagReady[id].status = "delete";
			} else if (this.tagReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.tagReady[id].status = "modified";
			} else {
				this.tagReady[id].status = operation;
			}
			this.tagReady[id].data = { ...this.tagEdition[id] };
			return { success: true, newStatus: this.tagReady[id].status };
		},
		getAvailableEditionTag() {
			// search existing "new{id}" in tagEdition to find available id for new TAG
			const newIds = Math.max(Object.keys(this.tagEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearTagEdition() {
			this.tagEdition = {};
			this.tagReady = {};
		},
		clearTagEditionById(id) {
			delete this.tagEdition[id];
			delete this.tagReady[id];
			this.clearTagStoreEdition(id);
			this.clearTagBoxEdition(id);
			this.clearTagItemEdition(id);
		},

		async getTagStoreByInterval(idTag, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.tagsStore[idTag] || clear) {
				this.tagsStore[idTag] = {};
			}
			this.tagsStoreLoading = true;
			const storesStore = useStoresStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newTagStoreList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/store?${paramString}`,
				useToken: "access",
			});
			for (const tagStore of newTagStoreList["data"]) {
				this.tagsStore[idTag][tagStore.id_store] = tagStore;
				if (expand.includes("store")) {
					storesStore.stores[tagStore.id_store] = tagStore.store;
				}
			}
			this.tagsStoreTotalCount[idTag] = newTagStoreList["pagination"]?.["total"] || 0;
			this.tagsStoreLoading = false;
			return [newTagStoreList["pagination"]?.["nextOffset"] || 0, newTagStoreList["pagination"]?.["hasMore"] || false];
		},
		async getTagStoreById(idTag, idStore, expand = []) {
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			if (!this.tagsStore[idTag][idStore]) {
				this.tagsStore[idTag][idStore] = {};
			}
			this.tagsStore[idTag][idStore].loading = true;
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsStore[idTag][idStore] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/store/${idStore}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("store")) {
				storesStore.stores[this.tagsStore[idTag].id_store] = this.tagsStore[idTag].store;
			}
		},
		async createTagStore(idTag, params) {
			const tagStore = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/store`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			this.tagsStore[idTag][tagStore.id_store] = tagStore;
		},
		async deleteTagStore(idTag, idStore) {
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/store/${idStore}`,
				useToken: "access",
			});
			delete this.tagsStore[idTag][idStore];
		},
		async createTagStoreBulk(idTag, params) {
			const tagStoreBulk = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/store/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			for (const tagStore of tagStoreBulk["valide"]) {
				this.tagsStore[idTag][tagStore.id_store] = tagStore;
			}
		},
		async deleteTagStoreBulk(idTag, params) {
			const tagStoreBulk = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/store/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagStore of tagStoreBulk["valide"]) {
				delete this.tagsStore[idTag][tagStore.id_store];
			}
		},
		async pushTagStoreReady(idTag) {
			if (!this.tagStoreReady[idTag]) {
				return;
			}
			for (const id in this.tagStoreReady[idTag]) {
				const change = this.tagStoreReady[idTag][id];
				if (change.status === "new") {
					await this.createTagStore(idTag, change.data);
					delete this.tagStoreReady[id];
				} else if (change.status === "modified") {
					await this.updateTagStore(idTag, id, change.data);
					delete this.tagStoreReady[id];
				} else if (change.status === "delete") {
					await this.deleteTagStore(idTag, id);
					delete this.tagStoreReady[id];
				}
			}
		},
		commitTagStoreEdition(idTag, id, operation = "modified") {
			if (!this.tagStoreEdition[idTag] || !this.tagStoreEdition[idTag][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.tagStoreReady[idTag]) {
				this.tagStoreReady[idTag] = {};
			}
			if (!this.tagStoreReady[idTag][id]) {
				this.tagStoreReady[idTag][id] = {};
				this.tagStoreReady[idTag][id].data = { id_store: id };
			}
			if (this.tagStoreReady[idTag][id].status === "new" && operation === "delete") {
				delete this.tagStoreReady[idTag][id];
				return { success: true, newStatus: "delete" };
			} else if (this.tagStoreReady[idTag][id].status === "modified" && operation === "delete") {
				this.tagStoreReady[idTag][id].status = "delete";
			} else if (this.tagStoreReady[idTag][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.tagStoreReady[idTag][id].status = "modified";
			} else {
				this.tagStoreReady[idTag][id].status = operation;
			}
			this.tagStoreReady[idTag][id].data = { ...this.tagStoreEdition[idTag][id] };
			return { success: true, newStatus: this.tagStoreReady[id].status };
		},
		getAvailableEditionTagStore() {
			// search existing "new{id}" in tagEdition to find available id for new TAG
			const newIds = Math.max(Object.keys(this.tagStoreEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearTagStoreEdition(idTag) {
			this.tagStoreEdition[idTag] = {};
			this.tagStoreReady[idTag] = {};
		},

		async getTagBoxByInterval(idTag, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.tagsBox[idTag] || clear) {
				this.tagsBox[idTag] = {};
			}
			this.tagsBoxLoading = true;
			const storesStore = useStoresStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newTagBoxList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/box?${paramString}`,
				useToken: "access",
			});
			for (const tagBox of newTagBoxList["data"]) {
				this.tagsBox[idTag][tagBox.id_box] = tagBox;
				if (expand.includes("box")) {
					storesStore.boxs[tagBox.id_box] = tagBox.box;
				}
			}
			this.tagsBoxTotalCount[idTag] = newTagBoxList["pagination"]?.["total"] || 0;
			this.tagsBoxLoading = false;
			return [newTagBoxList["pagination"]?.["nextOffset"] || 0, newTagBoxList["pagination"]?.["hasMore"] || false];
		},
		async getTagBoxById(idTag, idBox, expand = []) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			if (!this.tagsBox[idTag][idBox]) {
				this.tagsBox[idTag][idBox] = {};
			}
			this.tagsBox[idTag][idBox].loading = true;
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsBox[idTag][idBox] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/box/${idBox}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("box")) {
				storesStore.boxs[this.tagsBox[idTag].id_box] = this.tagsBox[idTag].box;
			}
		},
		async createTagBox(idTag, params) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			const tagBox = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/box`,
				useToken: "access",
				body: params,
			});
			this.tagsBox[idTag][tagBox.id_box] = tagBox;
		},
		async deleteTagBox(idTag, idBox) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/box/${idBox}`,
				useToken: "access",
			});
			delete this.tagsBox[idTag][idBox];
		},
		async createTagBoxBulk(idTag, params) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			const tagBoxBulk = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagBox of tagBoxBulk["valide"]) {
				this.tagsBox[idTag][tagBox.id_box] = tagBox;
			}
		},
		async deleteTagBoxBulk(idTag, params) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			const tagBoxBulk = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagBox of tagBoxBulk["valide"]) {
				delete this.tagsBox[idTag][tagBox.id_box];
			}
		},
		async pushTagBoxReady(idTag) {
			if (!this.tagBoxReady[idTag]) {
				return;
			}
			for (const id in this.tagBoxReady[idTag]) {
				const change = this.tagBoxReady[idTag][id];
				if (change.status === "new") {
					await this.createTagBox(idTag, change.data);
					delete this.tagBoxReady[id];
				} else if (change.status === "modified") {
					await this.updateTagBox(idTag, id, change.data);
					delete this.tagBoxReady[id];
				} else if (change.status === "delete") {
					await this.deleteTagBox(idTag, id);
					delete this.tagBoxReady[id];
				}
			}
		},
		commitTagBoxEdition(idTag, id, operation = "modified") {
			if (!this.tagBoxEdition[idTag] || !this.tagBoxEdition[idTag][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.tagBoxReady[idTag]) {
				this.tagBoxReady[idTag] = {};
			}
			if (!this.tagBoxReady[idTag][id]) {
				this.tagBoxReady[idTag][id] = {};
				this.tagReady[idTag][id].data = { id_box: id };
			}
			if (this.tagBoxReady[idTag][id].status === "new" && operation === "delete") {
				delete this.tagBoxReady[idTag][id];
				return { success: true, newStatus: "delete" };
			} else if (this.tagBoxReady[idTag][id].status === "modified" && operation === "delete") {
				this.tagBoxReady[idTag][id].status = "delete";
			} else if (this.tagBoxReady[idTag][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.tagBoxReady[idTag][id].status = "modified";
			} else {
				this.tagBoxReady[idTag][id].status = operation;
			}
			this.tagBoxReady[idTag][id].data = { ...this.tagBoxEdition[idTag][id] };
			return { success: true, newStatus: this.tagBoxReady[id].status };
		},
		getAvailableEditionTagBox() {
			// search existing "new{id}" in tagEdition to find available id for new TAG
			const newIds = Math.max(Object.keys(this.tagBoxEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearTagBoxEdition(idTag) {
			this.tagBoxEdition[idTag] = {};
			this.tagBoxReady[idTag] = {};
		},

		async getTagItemByInterval(idTag, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.tagsItem[idTag] || clear) {
				this.tagsItem[idTag] = {};
			}
			this.tagsItemLoading = true;
			const itemsStore = useItemsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newTagItemList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/item?${paramString}`,
				useToken: "access",
			});
			for (const tagItem of newTagItemList["data"]) {
				this.tagsItem[idTag][tagItem.id_item] = tagItem;
				if (expand.includes("item")) {
					itemsStore.items[tagItem.id_item] = tagItem.item;
				}
			}
			this.tagsItemTotalCount[idTag] = newTagItemList["pagination"]?.["total"] || 0;
			this.tagsItemLoading = false;
			return [newTagItemList["pagination"]?.["nextOffset"] || 0, newTagItemList["pagination"]?.["hasMore"] || false];
		},
		async getTagItemById(idTag, idItem, expand = []) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			if (!this.tagsItem[idTag][idItem]) {
				this.tagsItem[idTag][idItem] = {};
			}
			this.tagsItem[idTag][idItem].loading = true;
			const itemsStore = useItemsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsItem[idTag][idItem] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/item/${idItem}&${expandString}`,
				useToken: "access",
			});
			if (expand.includes("item")) {
				itemsStore.items[this.tagsItem[idTag].id_item] = this.tagsItem[idTag].item;
			}
		},
		async createTagItem(idTag, params) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			const tagItem = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/item`,
				useToken: "access",
				body: params,
			});
			this.tagsItem[idTag][tagItem.id_item] = tagItem;
		},
		async deleteTagItem(idTag, idItem) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/item/${idItem}`,
				useToken: "access",
			});
			delete this.tagsItem[idTag][idItem];
		},
		async createTagItemBulk(idTag, params) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			const tagItemBulk = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/item/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagItem of tagItemBulk["valide"]) {
				this.tagsItem[idTag][tagItem.id_item] = tagItem;
			}
		},
		async deleteTagItemBulk(idTag, params) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			const tagItemBulk = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/item/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagItem of tagItemBulk["valide"]) {
				delete this.tagsItem[idTag][tagItem.id_item];
			}
		},
		async pushTagItemReady(idTag) {
			if (!this.tagItemReady[idTag]) {
				return;
			}
			for (const id in this.tagItemReady[idTag]) {
				const change = this.tagItemReady[idTag][id];
				if (change.status === "new") {
					await this.createTagItem(idTag, change.data);
					delete this.tagItemReady[id];
				} else if (change.status === "delete") {
					await this.deleteTagItem(idTag, id);
					delete this.tagItemReady[id];
				}
			}
		},
		commitTagItemEdition(idTag, id, operation = "modified") {
			if (!this.tagItemEdition[idTag] || !this.tagItemEdition[idTag][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.tagItemReady[idTag]) {
				this.tagItemReady[idTag] = {};
			}
			if (!this.tagItemReady[idTag][id]) {
				this.tagItemReady[idTag][id] = {};
				this.tagItemReady[idTag][id].data = { id_item: id };
				this.tagItemReady[idTag][id].status = operation;
			}
			if (this.tagItemReady[idTag][id].status === "new" && operation === "delete") {
				delete this.tagItemReady[idTag][id];
				delete this.tagItemEdition[idTag][id];
				return { success: true, newStatus: "delete" };
			} else if (this.tagItemReady[idTag][id].status === "delete" && operation === "new") {
				delete this.tagItemReady[idTag][id];
				delete this.tagItemEdition[idTag][id];
				return { success: true, newStatus: "unchanged" };
			} else {
				this.tagItemReady[idTag][id].status = operation;
			}
			this.tagItemReady[idTag][id].data = { ...this.tagItemEdition[idTag][id] };
			delete this.tagItemEdition[idTag][id];
			return { success: true, newStatus: this.tagItemReady[idTag][id].status };
		},
		getAvailableEditionTagItem() {
			// search existing "new{id}" in tagEdition to find available id for new TAG
			const newIds = Math.max(Object.keys(this.tagItemEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearTagItemEdition(idTag) {
			this.tagItemEdition[idTag] = {};
			this.tagItemReady[idTag] = {};
		},
	},
});
