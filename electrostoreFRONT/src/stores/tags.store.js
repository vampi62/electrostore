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
			let newTagList = await fetchWrapper.get({
				url: `${baseUrl}/tag?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.tags[tag.id_tag] = tag;
				this.tagsStoreTotalCount[tag.id_tag] = tag.stores_tags_count;
				this.tagsBoxTotalCount[tag.id_tag] = tag.boxs_tags_count;
				this.tagsItemTotalCount[tag.id_tag] = tag.items_tags_count;
				if (expand.indexOf("stores_tags") > -1) {
					this.tagsStore[tag.id_tag] = {};
					for (const tagStore of tag.stores_tags) {
						this.tagsStore[tag.id_tag][tagStore.id_store] = tagStore;
					}
				}
				if (expand.indexOf("boxs_tags") > -1) {
					this.tagsBox[tag.id_tag] = {};
					for (const tagBox of tag.boxs_tags) {
						this.tagsBox[tag.id_tag][tagBox.id_box] = tagBox;
					}
				}
				if (expand.indexOf("items_tags") > -1) {
					this.tagsItem[tag.id_tag] = {};
					for (const tagItem of tag.items_tags) {
						this.tagsItem[tag.id_tag][tagItem.id_item] = tagItem;
					}
				}
			}
			this.tagsTotalCount = newTagList["count"];
			this.tagsLoading = false;
		},
		async getTagByInterval(limit = 100, offset = 0, expand = []) {
			this.tagsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newTagList = await fetchWrapper.get({
				url: `${baseUrl}/tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const tag of newTagList["data"]) {
				this.tags[tag.id_tag] = tag;
				this.tagsStoreTotalCount[tag.id_tag] = tag.stores_tags_count;
				this.tagsBoxTotalCount[tag.id_tag] = tag.boxs_tags_count;
				this.tagsItemTotalCount[tag.id_tag] = tag.items_tags_count;
				if (expand.indexOf("stores_tags") > -1) {
					this.tagsStore[tag.id_tag] = {};
					for (const tagStore of tag.stores_tags) {
						this.tagsStore[tag.id_tag][tagStore.id_store] = tagStore;
					}
				}
				if (expand.indexOf("boxs_tags") > -1) {
					this.tagsBox[tag.id_tag] = {};
					for (const tagBox of tag.boxs_tags) {
						this.tagsBox[tag.id_tag][tagBox.id_box] = tagBox;
					}
				}
				if (expand.indexOf("items_tags") > -1) {
					this.tagsItem[tag.id_tag] = {};
					for (const tagItem of tag.items_tags) {
						this.tagsItem[tag.id_tag][tagItem.id_item] = tagItem;
					}
				}
			}
			this.tagsTotalCount = newTagList["count"];
			this.tagsLoading = false;
		},
		async getTagById(id, expand = []) {
			if (!this.tags[id]) {
				this.tags[id] = {};
			}
			this.tags[id] = { ...this.tags[id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tags[id] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			this.tagsStoreTotalCount[id] = this.tags[id].stores_tags_count;
			this.tagsBoxTotalCount[id] = this.tags[id].boxs_tags_count;
			this.tagsItemTotalCount[id] = this.tags[id].items_tags_count;
			if (expand.indexOf("stores_tags") > -1) {
				this.tagsStore[id] = {};
				for (const tagStore of this.tags[id].stores_tags) {
					this.tagsStore[id][tagStore.id_store] = tagStore;
				}
			}
			if (expand.indexOf("boxs_tags") > -1) {
				this.tagsBox[id] = {};
				for (const tagBox of this.tags[id].boxs_tags) {
					this.tagsBox[id][tagBox.id_box] = tagBox;
				}
			}
			if (expand.indexOf("items_tags") > -1) {
				this.tagsItem[id] = {};
				for (const tagItem of this.tags[id].items_tags) {
					this.tagsItem[id][tagItem.id_item] = tagItem;
				}
			}
		},
		async createTag(params) {
			this.tagEdition.loading = true;
			this.tagEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag`,
				useToken: "access",
				body: params,
			});
			this.tags[this.tagEdition.id_tag] = this.tagEdition;
		},
		async updateTag(id, params) {
			this.tagEdition.loading = true;
			if (params.nom_tag === this.tags[id].nom_tag) {
				delete params.nom_tag;
			}
			this.tagEdition = await fetchWrapper.put({
				url: `${baseUrl}/tag/${id}`,
				useToken: "access",
				body: params,
			});
			this.tags[id] = this.tagEdition;
		},
		async deleteTag(id) {
			this.tagEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${id}`,
				useToken: "access",
			});
			delete this.tags[id];
			this.tagEdition = {};
		},
		async createTagBulk(params) {
			this.tagEdition.loading = true;
			this.tagEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tag of this.tagEdition["valide"]) {
				this.tags[tag.id_tag] = tag;
			}
		},

		async getTagStoreByInterval(idTag, limit = 100, offset = 0, expand = []) {
			this.tagsStoreLoading = true;
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			let newTagStoreList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/store?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			this.tagsStoreTotalCount[idTag] = newTagStoreList["count"];
			for (const tagStore of newTagStoreList["data"]) {
				this.tagsStore[idTag][tagStore.id_store] = tagStore;
				if (expand.indexOf("store") > -1) {
					storesStore.stores[tagStore.id_store] = tagStore.store;
				}
			}
			this.tagsStoreLoading = false;
		},
		async getTagStoreById(idTag, idStore, expand = []) {
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			if (!this.tagsStore[idTag][idStore]) {
				this.tagsStore[idTag][idStore] = {};
			}
			this.tagsStore[idTag][idStore] = { ...this.tagsStore[idTag][idStore], loading: true };
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsStore[idTag][idStore] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/store/${idStore}?${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("store") > -1) {
				storesStore.stores[this.tagsStore[idTag].id_store] = this.tagsStore[idTag].store;
			}
		},
		async createTagStore(idTag, params) {
			this.tagStoreEdition.loading = true;
			this.tagStoreEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/store`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			this.tagsStore[idTag][params.id_store] = this.tagStoreEdition;
		},
		async deleteTagStore(idTag, idStore) {
			this.tagStoreEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/store/${idStore}`,
				useToken: "access",
			});
			delete this.tagsStore[idTag][idStore];
			this.tagStoreEdition = {};
		},
		async createTagStoreBulk(idTag, params) {
			this.tagStoreEdition.loading = true;
			this.tagStoreEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/store/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsStore[idTag]) {
				this.tagsStore[idTag] = {};
			}
			for (const tagStore of this.tagStoreEdition["valide"]) {
				this.tagsStore[idTag][tagStore.id_store] = tagStore;
			}
		},
		async deleteTagStoreBulk(idTag, params) {
			this.tagStoreEdition.loading = true;
			this.tagStoreEdition = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/store/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagStore of this.tagStoreEdition["valide"]) {
				delete this.tagsStore[idTag][tagStore.id_store];
			}
		},

		async getTagBoxByInterval(idTag, limit = 100, offset = 0, expand = []) {
			this.tagsBoxLoading = true;
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			let newTagBoxList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/box?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			this.tagsBoxTotalCount[idTag] = newTagBoxList["count"];
			for (const tagBox of newTagBoxList["data"]) {
				this.tagsBox[idTag][tagBox.id_box] = tagBox;
				if (expand.indexOf("box") > -1) {
					storesStore.boxs[tagBox.id_box] = tagBox.box;
				}
			}
			this.tagsBoxLoading = false;
		},
		async getTagBoxById(idTag, idBox, expand = []) {
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			if (!this.tagsBox[idTag][idBox]) {
				this.tagsBox[idTag][idBox] = {};
			}
			this.tagsBox[idTag][idBox] = { ...this.tagsBox[idTag][idBox], loading: true };
			const storesStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsBox[idTag][idBox] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/box/${idBox}?${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("box") > -1) {
				storesStore.boxs[this.tagsBox[idTag].id_box] = this.tagsBox[idTag].box;
			}
		},
		async createTagBox(idTag, params) {
			this.tagBoxEdition.loading = true;
			this.tagBoxEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/box`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			this.tagsBox[idTag][params.id_box] = this.tagBoxEdition;
		},
		async deleteTagBox(idTag, idBox) {
			this.tagBoxEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/box/${idBox}`,
				useToken: "access",
			});
			delete this.tagsBox[idTag][idBox];
			this.tagBoxEdition = {};
		},
		async createTagBoxBulk(idTag, params) {
			this.tagBoxEdition.loading = true;
			this.tagBoxEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/box/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsBox[idTag]) {
				this.tagsBox[idTag] = {};
			}
			for (const tagBox of this.tagBoxEdition["valide"]) {
				this.tagsBox[idTag][tagBox.id_box] = tagBox;
			}
		},
		async deleteTagBoxBulk(idTag, params) {
			this.tagBoxEdition.loading = true;
			this.tagBoxEdition = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/box/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagBox of this.tagBoxEdition["valide"]) {
				delete this.tagsBox[idTag][tagBox.id_box];
			}
		},

		async getTagItemByInterval(idTag, limit = 100, offset = 0, expand = []) {
			this.tagsItemLoading = true;
			const itemsStore = useItemsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			let newTagItemList = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/item?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			this.tagsItemTotalCount[idTag] = newTagItemList["count"];
			for (const tagItem of newTagItemList["data"]) {
				this.tagsItem[idTag][tagItem.id_item] = tagItem;
				if (expand.indexOf("item") > -1) {
					itemsStore.items[tagItem.id_item] = tagItem.item;
				}
			}
			this.tagsItemLoading = false;
		},
		async getTagItemById(idTag, idItem, expand = []) {
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			if (!this.tagsItem[idTag][idItem]) {
				this.tagsItem[idTag][idItem] = {};
			}
			this.tagsItem[idTag][idItem] = { ...this.tagsItem[idTag][idItem], loading: true };
			const itemsStore = useItemsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.tagsItem[idTag][idItem] = await fetchWrapper.get({
				url: `${baseUrl}/tag/${idTag}/item/${idItem}&${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("item") > -1) {
				itemsStore.items[this.tagsItem[idTag].id_item] = this.tagsItem[idTag].item;
			}
		},
		async createTagItem(idTag, params) {
			this.tagItemEdition.loading = true;
			this.tagItemEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/item`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			this.tagsItem[idTag][params.id_item] = this.tagItemEdition;
		},
		async deleteTagItem(idTag, idItem) {
			this.tagItemEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/item/${idItem}`,
				useToken: "access",
			});
			delete this.tagsItem[idTag][idItem];
			this.tagItemEdition = {};
		},
		async createTagItemBulk(idTag, params) {
			this.tagItemEdition.loading = true;
			this.tagItemEdition = await fetchWrapper.post({
				url: `${baseUrl}/tag/${idTag}/item/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.tagsItem[idTag]) {
				this.tagsItem[idTag] = {};
			}
			for (const tagItem of this.tagItemEdition["valide"]) {
				this.tagsItem[idTag][tagItem.id_item] = tagItem;
			}
		},
		async deleteTagItemBulk(idTag, params) {
			this.tagItemEdition.loading = true;
			this.tagItemEdition = await fetchWrapper.delete({
				url: `${baseUrl}/tag/${idTag}/item/bulk`,
				useToken: "access",
				body: params,
			});
			for (const tagItem of this.tagItemEdition["valide"]) {
				delete this.tagsItem[idTag][tagItem.id_item];
			}
		},
	},
});
