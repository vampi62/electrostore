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

		tagsStoreLoading: true,
		tagsStoreTotalCount: {},
		tagsStore: {},
		tagStoreEdition: {},

		tagsBoxLoading: true,
		tagsBoxTotalCount: {},
		tagsBox: {},
		tagBoxEdition: {},

		tagsItemLoading: true,
		tagsItemTotalCount: {},
		tagsItem: {},
		tagItemEdition: {},
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
	},
});
