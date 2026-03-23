import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useTagsStore, useStoresStore, useCommandsStore, useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useItemsStore = defineStore("items",{
	state: () => ({
		itemsLoading: true,
		itemsTotalCount: 0,
		items: {},
		itemEdition: {},
		itemReady: {},

		documentsTotalCount: {},
		documentsLoading: false,
		documents: {},
		documentEdition: {},
		documentReady: {},

		itemBoxsTotalCount: {},
		itemBoxsLoading: false,
		itemBoxs: {},
		itemBoxEdition: {},
		itemBoxReady: {},

		itemTagsLoading: true,
		itemTagsTotalCount: {},
		itemTags: {},
		itemTagEdition: {},
		itemTagReady: {},

		itemCommandsLoading: true,
		itemCommandsTotalCount: {},
		itemCommands: {},
		itemCommandEdition: {},
		itemCommandReady: {},

		itemProjetsLoading: true,
		itemProjetsTotalCount: {},
		itemProjets: {},
		itemProjetEdition: {},
		itemProjetReady: {},

		imagesLoading: true,
		imagesTotalCount: {},
		images: {},
		imagesURL: {},
		thumbnailsURL: {},
		imageEdition: {},
		imageReady: {},
	}),
	actions: {
		async getItemByList(idResearch = [], expand = []) {
			this.itemsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				if (item.id_img && !this.thumbnailsURL[item.id_img]) {
					await this.showThumbnailById(item.id_item, item.id_img);
				}
				this.items[item.id_item] = item;
				this.documentsTotalCount[item.id_item] = item["item_documents_count"];
				this.itemBoxsTotalCount[item.id_item] = item["item_boxs_count"];
				this.itemTagsTotalCount[item.id_item] = item["item_tags_count"];
				this.itemCommandsTotalCount[item.id_item] = item["command_items_count"];
				this.itemProjetsTotalCount[item.id_item] = item["projet_items_count"];
				if (expand.includes("item_documents")) {
					this.documents[item.id_item] = {};
					for (const document of item["item_documents"]) {
						this.documents[item.id_item][document.id_item_document] = document;
					}
				}
				if (expand.includes("item_boxs")) {
					this.itemBoxs[item.id_item] = {};
					for (const itemBox of item["item_boxs"]) {
						this.itemBoxs[item.id_item][itemBox.id_box] = itemBox;
					}
				}
				if (expand.includes("item_tags")) {
					this.itemTags[item.id_item] = {};
					for (const itemTag of item["item_tags"]) {
						this.itemTags[item.id_item][itemTag.id_tag] = itemTag;
					}
				}
				if (expand.includes("command_items")) {
					this.itemCommands[item.id_item] = {};
					for (const itemCommand of item["command_items"]) {
						this.itemCommands[item.id_item][itemCommand.id_command] = itemCommand;
					}
				}
				if (expand.includes("projet_items")) {
					this.itemProjets[item.id_item] = {};
					for (const itemProjet of item["projet_items"]) {
						this.itemProjets[item.id_item][itemProjet.id_projet] = itemProjet;
					}
				}
			}
			this.itemsLoading = false;
		},
		async getItemByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.itemsLoading = true;
			if (clear) {
				this.items = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				if (item.id_img && !this.thumbnailsURL[item.id_img]) {
					await this.showThumbnailById(item.id_item, item.id_img);
				}
				this.items[item.id_item] = item;
				this.documentsTotalCount[item.id_item] = item["item_documents_count"];
				this.itemBoxsTotalCount[item.id_item] = item["item_boxs_count"];
				this.itemTagsTotalCount[item.id_item] = item["item_tags_count"];
				this.itemCommandsTotalCount[item.id_item] = item["command_items_count"];
				this.itemProjetsTotalCount[item.id_item] = item["projet_items_count"];
				if (expand.includes("item_documents")) {
					this.documents[item.id_item] = {};
					for (const document of item["item_documents"]) {
						this.documents[item.id_item][document.id_item_document] = document;
					}
				}
				if (expand.includes("item_boxs")) {
					this.itemBoxs[item.id_item] = {};
					for (const itemBox of item["item_boxs"]) {
						this.itemBoxs[item.id_item][itemBox.id_box] = itemBox;
					}
				}
				if (expand.includes("item_tags")) {
					this.itemTags[item.id_item] = {};
					for (const itemTag of item["item_tags"]) {
						this.itemTags[item.id_item][itemTag.id_tag] = itemTag;
					}
				}
				if (expand.includes("command_items")) {
					this.itemCommands[item.id_item] = {};
					for (const itemCommand of item["command_items"]) {
						this.itemCommands[item.id_item][itemCommand.id_command] = itemCommand;
					}
				}
				if (expand.includes("projet_items")) {
					this.itemProjets[item.id_item] = {};
					for (const itemProjet of item["projet_items"]) {
						this.itemProjets[item.id_item][itemProjet.id_projet] = itemProjet;
					}
				}
			}
			this.itemsTotalCount = newItemList["pagination"]?.["total"] || 0;
			this.itemsLoading = false;
			return [newItemList["pagination"]?.["nextOffset"] || 0, newItemList["pagination"]?.["hasMore"] || false];
		},
		async getItemById(id, expand = []) {
			if (!this.items[id]) {
				this.items[id] = {};
			}
			this.items[id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.items[id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${id}?${expandString}`,
				useToken: "access",
			});
			if (this.items[id].id_img && !this.thumbnailsURL[this.items[id].id_img]) {
				await this.showThumbnailById(id, this.items[id].id_img);
			}
			this.documentsTotalCount[id] = this.items[id]["item_documents_count"];
			this.itemBoxsTotalCount[id] = this.items[id]["item_boxs_count"];
			this.itemTagsTotalCount[id] = this.items[id]["item_tags_count"];
			this.itemCommandsTotalCount[id] = this.items[id]["command_items_count"];
			this.itemProjetsTotalCount[id] = this.items[id]["projet_items_count"];
			if (expand.includes("item_documents")) {
				this.documents[id] = {};
				for (const document of this.items[id]["item_documents"]) {
					this.documents[id][document.id_item_document] = document;
				}
			}
			if (expand.includes("item_boxs")) {
				this.itemBoxs[id] = {};
				for (const itemBox of this.items[id]["item_boxs"]) {
					this.itemBoxs[id][itemBox.id_box] = itemBox;
				}
			}
			if (expand.includes("item_tags")) {
				this.itemTags[id] = {};
				for (const itemTag of this.items[id]["item_tags"]) {
					this.itemTags[id][itemTag.id_tag] = itemTag;
				}
			}
			if (expand.includes("command_items")) {
				this.itemCommands[id] = {};
				for (const itemCommand of this.items[id]["command_items"]) {
					this.itemCommands[id][itemCommand.id_command] = itemCommand;
				}
			}
			if (expand.includes("projet_items")) {
				this.itemProjets[id] = {};
				for (const itemProjet of this.items[id]["projet_items"]) {
					this.itemProjets[id][itemProjet.id_projet] = itemProjet;
				}
			}
		},
		async createItem(params) {
			const item = await fetchWrapper.post({
				url: `${baseUrl}/item`,
				useToken: "access",
				body: params,
			});
			this.items[item.id_item] = item;
			return item.id_item;
		},
		async updateItem(id, params) {
			this.items[id] = await fetchWrapper.put({
				url: `${baseUrl}/item/${id}`,
				useToken: "access",
				body: params,
			});
			if (this.items[id].id_img) {
				await this.showImageById(this.items[id].id_item, this.items[id].id_img);
			}
		},
		async deleteItem(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${id}`,
				useToken: "access",
			});
			this.items[id] = null;
		},
		async pushItemReady() {
			for (const id in this.itemReady) {
				const change = this.itemReady[id];
				if (change.status === "new") {
					const newId = await this.createItem(change.data);
					this.updateIdItem(id, newId);
					this.pushDocumentReady(newId);
					this.pushItemBoxReady(newId);
					this.pushItemTagReady(newId);
					this.pushItemCommandReady(newId);
					this.pushItemProjetReady(newId);
					this.pushImageReady(newId);
					delete this.itemReady[id];
				} else if (change.status === "modified") {
					await this.updateItem(id, change.data);
					this.pushDocumentReady(id);
					this.pushItemBoxReady(id);
					this.pushItemTagReady(id);
					this.pushItemCommandReady(id);
					this.pushItemProjetReady(id);
					this.pushImageReady(id);
					delete this.itemReady[id];
				} else if (change.status === "delete") {
					await this.deleteItem(id);
					delete this.itemReady[id];
				}
			}
		},
		async pushItemById(id) {
			if (!this.itemEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createItem(this.itemEdition[id]);
				this.updateIdItem(id, newId);
			} else {
				await this.updateItem(id, this.itemEdition[id]);
			}
			this.pushDocumentReady(newId);
			this.pushItemBoxReady(newId);
			this.pushItemTagReady(newId);
			this.pushItemCommandReady(newId);
			this.pushItemProjetReady(newId);
			this.pushImageReady(newId);
			return newId;
		},
		updateIdItem(oldId, newId) {
			if (this.documentReady[oldId]) {
				this.documentReady[newId] = { ...this.documentReady[oldId], id_item: newId };
				delete this.documentReady[oldId];
			}
			if (this.documentEdition[oldId]) {
				this.documentEdition[newId] = { ...this.documentEdition[oldId], id_item: newId };
				delete this.documentEdition[oldId];
			}
			if (this.itemBoxReady[oldId]) {
				this.itemBoxReady[newId] = { ...this.itemBoxReady[oldId], id_item: newId };
				delete this.itemBoxReady[oldId];
			}
			if (this.itemBoxEdition[oldId]) {
				this.itemBoxEdition[newId] = { ...this.itemBoxEdition[oldId], id_item: newId };
				delete this.itemBoxEdition[oldId];
			}
			if (this.itemTagReady[oldId]) {
				this.itemTagReady[newId] = { ...this.itemTagReady[oldId], id_item: newId };
				delete this.itemTagReady[oldId];
			}
			if (this.itemTagEdition[oldId]) {
				this.itemTagEdition[newId] = { ...this.itemTagEdition[oldId], id_item: newId };
				delete this.itemTagEdition[oldId];
			}
			if (this.itemCommandReady[oldId]) {
				this.itemCommandReady[newId] = { ...this.itemCommandReady[oldId], id_item: newId };
				delete this.itemCommandReady[oldId];
			}
			if (this.itemCommandEdition[oldId]) {
				this.itemCommandEdition[newId] = { ...this.itemCommandEdition[oldId], id_item: newId };
				delete this.itemCommandEdition[oldId];
			}
			if (this.itemProjetReady[oldId]) {
				this.itemProjetReady[newId] = { ...this.itemProjetReady[oldId], id_item: newId };
				delete this.itemProjetReady[oldId];
			}
			if (this.itemProjetEdition[oldId]) {
				this.itemProjetEdition[newId] = { ...this.itemProjetEdition[oldId], id_item: newId };
				delete this.itemProjetEdition[oldId];
			}
			if (this.imageReady[oldId]) {
				this.imageReady[newId] = { ...this.imageReady[oldId], id_item: newId };
				delete this.imageReady[oldId];
			}
			if (this.imageEdition[oldId]) {
				this.imageEdition[newId] = { ...this.imageEdition[oldId], id_item: newId };
				delete this.imageEdition[oldId];
			}
		},
		commitItemEdition(id, operation = "modified") { // return (sucess:bool, newStatus:string)
			if (!this.itemEdition[id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemReady[id]) {
				this.itemReady[id] = {};
			}
			if (this.itemReady[id].status === "new" && operation === "delete") {
				delete this.itemReady[id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemReady[id].status === "modified" && operation === "delete") {
				this.itemReady[id].status = "delete";
			} else if (this.itemReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemReady[id].status = "modified";
			} else {
				this.itemReady[id].status = operation;
			}
			this.itemReady[id].data = { ...this.itemEdition[id] };
			return { success: true, newStatus: this.itemReady[id].status };
		},
		getAvailableEditionItem() {
			// search existing "new{id}" in itemEdition to find available id for new ITEM
			const newIds = Math.max(Object.keys(this.itemEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearItemEdition() {
			this.itemEdition = {};
			this.itemReady = {};
		},
		clearItemEditionById(id) {
			delete this.itemEdition[id];
			delete this.itemReady[id];
			this.clearDocumentEdition(id);
			this.clearItemBoxEdition(id);
			this.clearItemTagEdition(id);
			this.clearItemCommandEdition(id);
			this.clearItemProjetEdition(id);
			this.clearImageEdition(id);
		},

		async getDocumentByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.documents[idItem] || clear) {
				this.documents[idItem] = {};
			}
			this.documentsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/document?${paramString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idItem][document.id_item_document] = document;
			}
			this.documentsTotalCount[idItem] = newDocumentList["pagination"]?.["total"] || 0;
			this.documentsLoading = false;
			return [newDocumentList["pagination"]?.["nextOffset"] || 0, newDocumentList["pagination"]?.["hasMore"] || false];
		},
		async getDocumentById(idItem, id) {
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			if (!this.documents[idItem][id]) {
				this.documents[idItem][id] = {};
			}
			this.documents[idItem][id].loading = true;
			this.documents[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
			});
		},
		async createDocument(idItem, params) {
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			const formData = new FormData();
			formData.append("name_item_document", params.name_item_document);
			formData.append("document", params.document);
			const document = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/document`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.documents[idItem][document.id_item_document] = document;
		},
		async updateDocument(idItem, id, params) {
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			this.documents[idItem][id] = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteDocument(idItem, id) {
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
			});
			delete this.documents[idItem][id];
		},
		async downloadDocument(idItem, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/item/${idItem}/document/${id}/download`,
				useToken: "access",
			});
		},
		commitDocumentEdition(idItem, id, operation = "modified") {
			if (!this.documentEdition[idItem] || !this.documentEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.documentReady[idItem]) {
				this.documentReady[idItem] = {};
			}
			if (!this.documentReady[idItem][id]) {
				this.documentReady[idItem][id] = {};
			}
			if (this.documentReady[idItem][id].status === "new" && operation === "delete") {
				delete this.documentReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.documentReady[idItem][id].status === "modified" && operation === "delete") {
				this.documentReady[idItem][id].status = "delete";
			} else if (this.documentReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.documentReady[idItem][id].status = "modified";
			} else {
				this.documentReady[idItem][id].status = operation;
			}
			this.documentReady[idItem][id].data = { ...this.documentEdition[idItem][id] };
			return { success: true, newStatus: this.documentReady[id].status };
		},
		async pushDocumentReady(idItem) {
			if (!this.documentReady[idItem]) {
				return;
			}
			for (const id in this.documentReady[idItem]) {
				const change = this.documentReady[idItem][id];
				if (change.status === "new") {
					await this.createDocument(idItem, change.data);
					delete this.documentReady[id];
				} else if (change.status === "modified") {
					await this.updateDocument(idItem, id, change.data);
					delete this.documentReady[id];
				} else if (change.status === "delete") {
					await this.deleteDocument(idItem, id);
					delete this.documentReady[id];
				}
			}
		},
		clearDocumentEdition(idItem) {
			this.documentEdition[idItem] = {};
			this.documentReady[idItem] = {};
		},

		async getItemBoxByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.itemBoxs[idItem] || clear) {
				this.itemBoxs[idItem] = {};
			}
			this.itemBoxsLoading = true;
			const storeStore = useStoresStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemBoxList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/box?${paramString}`,
				useToken: "access",
			});
			for (const itemBox of newItemBoxList["data"]) {
				this.itemBoxs[idItem][itemBox.id_box] = itemBox;
				if (expand.includes("box")) {
					if (!storeStore.boxs[itemBox["box"].id_store]) {
						storeStore.boxs[itemBox["box"].id_store] = {};
					}
					storeStore.boxs[itemBox["box"].id_store][itemBox.id_box] = itemBox["box"];
				}
			}
			this.itemBoxsTotalCount[idItem] = newItemBoxList["pagination"]?.["total"] || 0;
			this.itemBoxsLoading = false;
			return [newItemBoxList["pagination"]?.["nextOffset"] || 0, newItemBoxList["pagination"]?.["hasMore"] || false];
		},
		async getItemBoxById(idItem, id, expand = []) {
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			if (!this.itemBoxs[idItem][id]) {
				this.itemBoxs[idItem][id] = {};
			}
			this.itemBoxs[idItem][id].loading = true;
			const storeStore = useStoresStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemBoxs[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/box/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("box")) {
				storeStore.boxs[this.itemBoxs[idItem][id].id_box] = this.itemBoxs[idItem][id]["box"];
			}
		},
		async createItemBox(idItem, params) {
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			const itemBox = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/box`,
				useToken: "access",
				body: params,
			});
			this.itemBoxs[idItem][itemBox.id_box] = itemBox;
		},
		async updateItemBox(idItem, id, params) {
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			this.itemBoxs[idItem][id] = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/box/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteItemBox(idItem, id) {
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/box/${id}`,
				useToken: "access",
			});
			delete this.itemBoxs[idItem][id];
		},
		commitItemBoxEdition(idItem, id, operation = "modified") {
			if (!this.itemBoxEdition[idItem] || !this.itemBoxEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemBoxReady[idItem]) {
				this.itemBoxReady[idItem] = {};
			}
			if (!this.itemBoxReady[idItem][id]) {
				this.itemBoxReady[idItem][id] = {};
			}
			if (this.itemBoxReady[idItem][id].status === "new" && operation === "delete") {
				delete this.itemBoxReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemBoxReady[idItem][id].status === "modified" && operation === "delete") {
				this.itemBoxReady[idItem][id].status = "delete";
			} else if (this.itemBoxReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemBoxReady[idItem][id].status = "modified";
			} else {
				this.itemBoxReady[idItem][id].status = operation;
			}
			this.itemBoxReady[idItem][id].data = { ...this.itemBoxEdition[idItem][id] };
			return { success: true, newStatus: this.itemBoxReady[id].status };
		},
		async pushItemBoxReady(idItem) {
			if (!this.itemBoxReady[idItem]) {
				return;
			}
			for (const id in this.itemBoxReady[idItem]) {
				const change = this.itemBoxReady[idItem][id];
				if (change.status === "new") {
					await this.createItemBox(idItem, change.data);
					delete this.itemBoxReady[id];
				} else if (change.status === "modified") {
					await this.updateItemBox(idItem, id, change.data);
					delete this.itemBoxReady[id];
				} else if (change.status === "delete") {
					await this.deleteItemBox(idItem, id);
					delete this.itemBoxReady[id];
				}
			}
		},
		clearItemBoxEdition(idItem) {
			this.itemBoxEdition[idItem] = {};
			this.itemBoxReady[idItem] = {};
		},

		async getItemTagByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.itemTags[idItem] || clear) {
				this.itemTags[idItem] = {};
			}
			this.itemTagsLoading = true;
			const tagsStore = useTagsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemTagList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/tag?${paramString}`,
				useToken: "access",
			});
			for (const itemTag of newItemTagList["data"]) {
				this.itemTags[idItem][itemTag.id_tag] = itemTag;
				if (expand.includes("tag")) {
					tagsStore.tags[itemTag.id_tag] = itemTag["tag"];
				}
			}
			this.itemTagsTotalCount[idItem] = newItemTagList["pagination"]?.["total"] || 0;
			this.itemTagsLoading = false;
			return [newItemTagList["pagination"]?.["nextOffset"] || 0, newItemTagList["pagination"]?.["hasMore"] || false];
		},
		async getItemTagById(idItem, id, expand = []) {
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			if (!this.itemTags[idItem][id]) {
				this.itemTags[idItem][id] = {};
			}
			this.itemTags[idItem][id].loading = true;
			const tagsStore = useTagsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemTags[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[this.itemTags[idItem][id].id_tag] = this.itemTags[idItem][id]["tag"];
			}
		},
		async createItemTag(idItem, params) {
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			const itemTag = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/tag`,
				useToken: "access",
				body: params,
			});
			this.itemTags[idItem][itemTag.id_tag] = itemTag;
		},
		async deleteItemTag(idItem, id) {
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/tag/${id}`,
				useToken: "access",
			});
			delete this.itemTags[idItem][id];
		},
		async createItemTagBulk(idItem, idList) {
			if (!this.itemTagEdition[idItem]) {
				this.itemTagEdition[idItem] = {};
			}
			const itemTagBulk = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/tag/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemTag of itemTagBulk["valide"]) {
				this.itemTags[idItem][itemTag.id_tag] = itemTag;
			}
		},
		async deleteItemTagBulk(idItem, idList) {
			if (!this.itemTagEdition[idItem]) {
				this.itemTagEdition[idItem] = {};
			}
			const itemTagBulk = await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/tag/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemTag of itemTagBulk["valide"]) {
				delete this.itemTags[idItem][itemTag.id_tag];
			}
		},
		commitItemTagEdition(idItem, id, operation = "modified") {
			if (!this.itemTagEdition[idItem] || !this.itemTagEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemTagReady[idItem]) {
				this.itemTagReady[idItem] = {};
			}
			if (!this.itemTagReady[idItem][id]) {
				this.itemTagReady[idItem][id] = {};
			}
			if (this.itemTagReady[idItem][id].status === "new" && operation === "delete") {
				delete this.itemTagReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemTagReady[idItem][id].status === "modified" && operation === "delete") {
				this.itemTagReady[idItem][id].status = "delete";
			} else if (this.itemTagReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemTagReady[idItem][id].status = "modified";
			} else {
				this.itemTagReady[idItem][id].status = operation;
			}
			this.itemTagReady[idItem][id].data = { ...this.itemTagEdition[idItem][id] };
			return { success: true, newStatus: this.itemTagReady[id].status };
		},
		async pushItemTagReady(idItem) {
			if (!this.itemTagReady[idItem]) {
				return;
			}
			for (const id in this.itemTagReady[idItem]) {
				const change = this.itemTagReady[idItem][id];
				if (change.status === "new") {
					await this.createItemTag(idItem, change.data);
					delete this.itemTagReady[id];
				} else if (change.status === "modified") {
					await this.updateItemTag(idItem, id, change.data);
					delete this.itemTagReady[id];
				} else if (change.status === "delete") {
					await this.deleteItemTag(idItem, id);
					delete this.itemTagReady[id];
				}
			}
		},
		clearItemTagEdition(idItem) {
			this.itemTagEdition[idItem] = {};
			this.itemTagReady[idItem] = {};
		},

		async getItemCommandByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.itemCommands[idItem] || clear) {
				this.itemCommands[idItem] = {};
			}
			this.itemCommandsLoading = true;
			const commandsStore = useCommandsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemCommandList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/command?${paramString}`,
				useToken: "access",
			});
			for (const itemCommand of newItemCommandList["data"]) {
				this.itemCommands[idItem][itemCommand.id_command] = itemCommand;
				if (expand.includes("command")) {
					commandsStore.commands[itemCommand.id_command] = itemCommand["command"];
				}
			}
			this.itemCommandsTotalCount[idItem] = newItemCommandList["pagination"]?.["total"] || 0;
			this.itemCommandsLoading = false;
			return [newItemCommandList["pagination"]?.["nextOffset"] || 0, newItemCommandList["pagination"]?.["hasMore"] || false];
		},
		async getItemCommandById(idItem, id, expand = []) {
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			if (!this.itemCommands[idItem][id]) {
				this.itemCommands[idItem][id] = {};
			}
			this.itemCommands[idItem][id].loading = true;
			const commandsStore = useCommandsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemCommands[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/command/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("command")) {
				commandsStore.commands[this.itemCommands[idItem][id].id_command] = this.itemCommands[idItem][id]["command"];
			}
		},
		async createItemCommand(idItem, params) {
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			const itemCommand = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/command`,
				useToken: "access",
				body: params,
			});
			this.itemCommands[idItem][itemCommand.id_command] = itemCommand;
		},
		async updateItemCommand(idItem, id, params) {
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			this.itemCommands[idItem][id] = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/command/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteItemCommand(idItem, id) {
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/command/${id}`,
				useToken: "access",
			});
			delete this.itemCommands[idItem][id];
		},
		async createItemCommandBulk(idItem, idList) {
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			const itemCommandBulk = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/command/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemCommand of itemCommandBulk["valide"]) {
				this.itemCommands[idItem][itemCommand.id_command] = itemCommand;
			}
		},
		commitItemCommandEdition(idItem, id, operation = "modified") {
			if (!this.itemCommandEdition[idItem] || !this.itemCommandEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemCommandReady[idItem]) {
				this.itemCommandReady[idItem] = {};
			}
			if (!this.itemCommandReady[idItem][id]) {
				this.itemCommandReady[idItem][id] = {};
			}
			if (this.itemCommandReady[idItem][id].status === "new" && operation === "delete") {
				delete this.itemCommandReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemCommandReady[idItem][id].status === "modified" && operation === "delete") {
				this.itemCommandReady[idItem][id].status = "delete";
			} else if (this.itemCommandReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemCommandReady[idItem][id].status = "modified";
			} else {
				this.itemCommandReady[idItem][id].status = operation;
			}
			this.itemCommandReady[idItem][id].data = { ...this.itemCommandEdition[idItem][id] };
			return { success: true, newStatus: this.itemCommandReady[id].status };
		},
		async pushItemCommandReady(idItem) {
			if (!this.itemCommandReady[idItem]) {
				return;
			}
			for (const id in this.itemCommandReady[idItem]) {
				const change = this.itemCommandReady[idItem][id];
				if (change.status === "new") {
					await this.createItemCommand(idItem, change.data);
					delete this.itemCommandReady[id];
				} else if (change.status === "modified") {
					await this.updateItemCommand(idItem, id, change.data);
					delete this.itemCommandReady[id];
				} else if (change.status === "delete") {
					await this.deleteItemCommand(idItem, id);
					delete this.itemCommandReady[id];
				}
			}
		},
		clearItemCommandEdition(idItem) {
			this.itemCommandEdition[idItem] = {};
			this.itemCommandReady[idItem] = {};
		},

		async getItemProjetByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.itemProjets[idItem] || clear) {
				this.itemProjets[idItem] = {};
			}
			this.itemProjetsLoading = true;
			const projetsStore = useProjetsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemProjetList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/projet?${paramString}`,
				useToken: "access",
			});
			for (const itemProjet of newItemProjetList["data"]) {
				this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
				if (expand.includes("projet")) {
					projetsStore.projets[itemProjet.id_projet] = itemProjet["projet"];
				}
			}
			this.itemProjetsTotalCount[idItem] = newItemProjetList["pagination"]?.["total"] || 0;
			this.itemProjetsLoading = false;
			return [newItemProjetList["pagination"]?.["nextOffset"] || 0, newItemProjetList["pagination"]?.["hasMore"] || false];
		},
		async getItemProjetById(idItem, id, expand = []) {
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			if (!this.itemProjets[idItem][id]) {
				this.itemProjets[idItem][id] = {};
			}
			this.itemProjets[idItem][id].loading = true;
			const projetsStore = useProjetsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemProjets[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/projet/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("projet")) {
				projetsStore.projets[this.itemProjets[idItem][id].id_projet] = this.itemProjets[idItem][id]["projet"];
			}
		},
		async createItemProjet(idItem, params) {
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			const itemProjet = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/projet`,
				useToken: "access",
				body: params,
			});
			this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
		},
		async updateItemProjet(idItem, id, params) {
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			this.itemProjets[idItem][id] = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/projet/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteItemProjet(idItem, id) {
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/projet/${id}`,
				useToken: "access",
			});
			delete this.itemProjets[idItem][id];
		},
		async createItemProjetBulk(idItem, idList) {
			if (!this.itemProjetEdition[idItem]) {
				this.itemProjetEdition[idItem] = {};
			}
			const itemProjetBulk = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/projet/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemProjet of itemProjetBulk["valide"]) {
				this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
			}
		},
		commitItemProjetEdition(idItem, id, operation = "modified") {
			if (!this.itemProjetEdition[idItem] || !this.itemProjetEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemProjetReady[idItem]) {
				this.itemProjetReady[idItem] = {};
			}
			if (!this.itemProjetReady[idItem][id]) {
				this.itemProjetReady[idItem][id] = {};
			}
			if (this.itemProjetReady[idItem][id].status === "new" && operation === "delete") {
				delete this.itemProjetReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemProjetReady[idItem][id].status === "modified" && operation === "delete") {
				this.itemProjetReady[idItem][id].status = "delete";
			} else if (this.itemProjetReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemProjetReady[idItem][id].status = "modified";
			} else {
				this.itemProjetReady[idItem][id].status = operation;
			}
			this.itemProjetReady[idItem][id].data = { ...this.itemProjetEdition[idItem][id] };
			return { success: true, newStatus: this.itemProjetReady[id].status };
		},
		async pushItemProjetReady(idItem) {
			if (!this.itemProjetReady[idItem]) {
				return;
			}
			for (const id in this.itemProjetReady[idItem]) {
				const change = this.itemProjetReady[idItem][id];
				if (change.status === "new") {
					await this.createItemProjet(idItem, change.data);
					delete this.itemProjetReady[id];
				} else if (change.status === "modified") {
					await this.updateItemProjet(idItem, id, change.data);
					delete this.itemProjetReady[id];
				} else if (change.status === "delete") {
					await this.deleteItemProjet(idItem, id);
					delete this.itemProjetReady[id];
				}
			}
		},
		clearItemProjetEdition(idItem) {
			this.itemProjetEdition[idItem] = {};
			this.itemProjetReady[idItem] = {};
		},

		async getImageByInterval(idItem, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false, loadThumbnails = true, loadImages = false) {
			if (!this.images[idItem] || clear) {
				this.images[idItem] = {};
			}
			this.imagesLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newImagesList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/img?${paramString}`,
				useToken: "access",
			});
			for (const img of newImagesList["data"]) {
				this.images[idItem][img.id_img] = img;
				if (loadImages && !this.imagesURL[img.id_img]) {
					this.showImageById(idItem, img.id_img);
				}
				if (loadThumbnails && !this.thumbnailsURL[img.id_img]) {
					this.showThumbnailById(idItem, img.id_img);
				}
			}
			this.imagesTotalCount[idItem] = newImagesList["pagination"]?.["total"] || 0;
			this.imagesLoading = false;
			return [newImagesList["pagination"]?.["nextOffset"] || 0, newImagesList["pagination"]?.["hasMore"] || false];
		},
		async getImageById(idItem, id, loadThumbnails = true, loadImages = false) {
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			if (!this.images[idItem][id]) {
				this.images[idItem][id] = {};
			}
			this.images[idItem][id].loading = true;
			this.images[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/img/${id}`,
				useToken: "access",
			});
			if (loadImages && !this.imagesURL[id]) {
				await this.showImageById(idItem, id);
			}
			if (loadThumbnails && !this.thumbnailsURL[id]) {
				await this.showThumbnailById(idItem, id);
			}
		},
		async createImage(idItem, params, loadThumbnails = true, loadImages = false) {
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			const formData = new FormData();
			formData.append("nom_img", params.nom_img);
			formData.append("description_img", params.description_img);
			formData.append("img_file", params.image);
			const image = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/img`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.images[idItem][image.id_img] = image;
			if (loadImages && !this.imagesURL[image.id_img]) {
				await this.showImageById(idItem, image.id_img);
			}
			if (loadThumbnails && !this.thumbnailsURL[image.id_img]) {
				await this.showThumbnailById(idItem, image.id_img);
			}
		},
		async updateImage(idItem, id_img, params, loadThumbnails = true, loadImages = false) {
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			this.images[idItem][id_img] = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/img/${id_img}`,
				useToken: "access",
				body: params,
			});
			if (loadImages && this.images[idItem][id_img]?.id_img && !this.imagesURL[this.images[idItem][id_img].id_img]) {
				await this.showImageById(idItem, this.images[idItem][id_img].id_img);
			}
			if (loadThumbnails && this.images[idItem][id_img]?.id_img && !this.thumbnailsURL[this.images[idItem][id_img].id_img]) {
				await this.showThumbnailById(idItem, this.images[idItem][id_img].id_img);
			}
		},
		async deleteImage(iditem, id_img) {
			if (!this.images[iditem]) {
				this.images[iditem] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${iditem}/img/${id_img}`,
				useToken: "access",
			});
			delete this.images[iditem][id_img];
			if (this.imagesURL[id_img]) {
				URL.revokeObjectURL(this.imagesURL[id_img]);
				delete this.imagesURL[id_img];
			}
			if (this.thumbnailsURL[id_img]) {
				URL.revokeObjectURL(this.thumbnailsURL[id_img]);
				delete this.thumbnailsURL[id_img];
			}
		},
		async showImageById(id_item, id_img) {
			if (this.imagesURL[id_img]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id_item}/img/${id_img}/picture`,
				useToken: "access",
			});
			const url = URL.createObjectURL(response);
			this.imagesURL[id_img] = url;
		},
		async showThumbnailById(id_item, id_img) {
			if (this.thumbnailsURL[id_img]) {
				return;
			}
			const response = await fetchWrapper.image({
				url: `${baseUrl}/item/${id_item}/img/${id_img}/thumbnail`,
				useToken: "access",
			});
			const url = URL.createObjectURL(response);
			this.thumbnailsURL[id_img] = url;
		},
		commitImageEdition(idItem, id, operation = "modified") {
			if (!this.imageEdition[idItem] || !this.imageEdition[idItem][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.imageReady[idItem]) {
				this.imageReady[idItem] = {};
			}
			if (!this.imageReady[idItem][id]) {
				this.imageReady[idItem][id] = {};
			}
			if (this.imageReady[idItem][id].status === "new" && operation === "delete") {
				delete this.imageReady[idItem][id];
				return { success: true, newStatus: "delete" };
			} else if (this.imageReady[idItem][id].status === "modified" && operation === "delete") {
				this.imageReady[idItem][id].status = "delete";
			} else if (this.imageReady[idItem][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.imageReady[idItem][id].status = "modified";
			} else {
				this.imageReady[idItem][id].status = operation;
			}
			this.imageReady[idItem][id].data = { ...this.imageEdition[idItem][id] };
			return { success: true, newStatus: this.imageReady[id].status };
		},
		async pushImageReady(idItem) {
			if (!this.imageReady[idItem]) {
				return;
			}
			for (const id in this.imageReady[idItem]) {
				const change = this.imageReady[idItem][id];
				if (change.status === "new") {
					await this.createImage(idItem, change.data);
					delete this.imageReady[id];
				} else if (change.status === "modified") {
					await this.updateImage(idItem, id, change.data);
					delete this.imageReady[id];
				} else if (change.status === "delete") {
					await this.deleteImage(idItem, id);
					delete this.imageReady[id];
				}
			}
		},
		clearImageEdition(idItem) {
			this.imageEdition[idItem] = {};
			this.imageReady[idItem] = {};
		},
	},
});
