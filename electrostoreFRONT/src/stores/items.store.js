import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useTagsStore, useStoresStore, useCommandsStore, useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useItemsStore = defineStore("items",{
	state: () => ({
		itemsLoading: false,
		itemsTotalCount: 0,
		items: {},
		itemEdition: {},

		documentsLoading: false,
		documentsTotalCount: {},
		documents: {},
		documentEdition: {},

		itemBoxsLoading: false,
		itemBoxsTotalCount: {},
		itemBoxs: {},
		itemBoxEdition: {},

		itemTagsLoading: false,
		itemTagsTotalCount: {},
		itemTags: {},
		itemTagEdition: {},

		itemCommandsLoading: false,
		itemCommandsTotalCount: {},
		itemCommands: {},
		itemCommandEdition: {},

		itemProjetsLoading: false,
		itemProjetsTotalCount: {},
		itemProjets: {},
		itemProjetEdition: {},

		imagesLoading: false,
		imagesTotalCount: {},
		images: {},
		imagesURL: {},
		thumbnailsURL: {},
		imageEdition: {},
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
			const itemTagBulk = await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/tag/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemTag of itemTagBulk["valide"]) {
				delete this.itemTags[idItem][itemTag.id_tag];
			}
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
			const itemProjetBulk = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/projet/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemProjet of itemProjetBulk["valide"]) {
				this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
			}
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
	},
});
