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

		documentsTotalCount: {},
		documentsLoading: false,
		documents: {},
		documentEdition: {},

		itemBoxsTotalCount: {},
		itemBoxsLoading: false,
		itemBoxs: {},
		itemBoxEdition: {},

		itemTagsLoading: true,
		itemTagsTotalCount: {},
		itemTags: {},
		itemTagEdition: {},

		itemCommandsLoading: true,
		itemCommandsTotalCount: {},
		itemCommands: {},
		itemCommandEdition: {},

		itemProjetsLoading: true,
		itemProjetsTotalCount: {},
		itemProjets: {},
		itemProjetEdition: {},

		imagesLoading: true,
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
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/item?${idResearchString}&${expandString}`,
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
			this.itemsTotalCount = newItemList["count"];
			this.itemsLoading = false;
		},
		async getItemByInterval(limit = 100, offset = 0, expand = []) {
			this.itemsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/item?limit=${limit}&offset=${offset}&${expandString}`,
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
			this.itemsTotalCount = newItemList["count"];
			this.itemsLoading = false;
		},
		async getItemById(id, expand = []) {
			if (!this.items[id]) {
				this.items[id] = {};
			}
			this.items[id] = { ...this.items[id], loading: true };
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
			this.itemEdition.loading = true;
			this.itemEdition = await fetchWrapper.post({
				url: `${baseUrl}/item`,
				useToken: "access",
				body: params,
			});
			this.items[this.itemEdition.id_item] = this.itemEdition;
		},
		async updateItem(id, params) {
			this.itemEdition.loading = true;
			this.itemEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${id}`,
				useToken: "access",
				body: params,
			});
			this.items[id] = this.itemEdition;
			if (this.itemEdition.id_img) {
				await this.showImageById(this.itemEdition.id_item, this.itemEdition.id_img);
			}
		},
		async deleteItem(id) {
			this.itemEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${id}`,
				useToken: "access",
			});
			this.items[id] = null;
			this.itemEdition = {};
		},

		async getDocumentByInterval(idItem, limit = 100, offset = 0, expand = []) {
			this.documentsLoading = true;
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/document?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idItem][document.id_item_document] = document;
			}
			this.documentsTotalCount[idItem] = newDocumentList["count"];
			this.documentsLoading = false;
		},
		async getDocumentById(idItem, id) {
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			if (!this.documents[idItem][id]) {
				this.documents[idItem][id] = {};
			}
			this.documents[idItem][id] = { ...this.documents[idItem][id], loading: true };
			this.documents[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
			});
		},
		async createDocument(idItem, params) {
			this.documentEdition.loading = true;
			const formData = new FormData();
			formData.append("name_item_document", params.name_item_document);
			formData.append("document", params.document);
			this.documentEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/document`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			if (!this.documents[idItem]) {
				this.documents[idItem] = {};
			}
			this.documents[idItem][this.documentEdition.id_item_document] = this.documentEdition;
		},
		async updateDocument(idItem, id, params) {
			this.documents[idItem][id].loading = true;
			this.documentEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
				body: params,
			});
			this.documents[idItem][id] = this.documentEdition;
		},
		async deleteDocument(idItem, id) {
			this.documentEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/document/${id}`,
				useToken: "access",
			});
			delete this.documents[idItem][id];
			this.documentEdition = {};
		},
		async downloadDocument(idItem, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/item/${idItem}/document/${id}/download`,
				useToken: "access",
			});
		},

		async getItemBoxByList(idItem, idResearch = [], expand = []) {
			this.itemBoxsLoading = true;
			const storeStore = useStoresStore();
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			let newItemBoxList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/box?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const itemBox of newItemBoxList["data"]) {
				this.itemBoxs[idItem][itemBox.id_box] = itemBox;
				if (expand.includes("box")) {
					storeStore.boxs[itemBox.id_box] = itemBox["box"];
				}
			}
			this.itemBoxsTotalCount[idItem] = newItemBoxList["count"];
			this.itemBoxsLoading = false;
		},
		async getItemBoxByInterval(idItem, limit = 100, offset = 0, expand = []) {
			this.itemBoxsLoading = true;
			const storeStore = useStoresStore();
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemBoxList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/box?limit=${limit}&offset=${offset}&${expandString}`,
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
			this.itemBoxsTotalCount[idItem] = newItemBoxList["count"];
			this.itemBoxsLoading = false;
		},
		async getItemBoxById(idItem, id, expand = []) {
			const storeStore = useStoresStore();
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			if (!this.itemBoxs[idItem][id]) {
				this.itemBoxs[idItem][id] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemBoxs[idItem][id] = { ...this.itemBoxs[idItem][id], loading: true };
			this.itemBoxs[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/box/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("box")) {
				storeStore.boxs[this.itemBoxs[idItem][id].id_box] = this.itemBoxs[idItem][id]["box"];
			}
		},
		async createItemBox(idItem, params) {
			this.itemBoxEdition.loading = true;
			this.itemBoxEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/box`,
				useToken: "access",
				body: params,
			});
			if (!this.itemBoxs[idItem]) {
				this.itemBoxs[idItem] = {};
			}
			this.itemBoxs[idItem][this.itemBoxEdition.id_box] = this.itemBoxEdition;
		},
		async updateItemBox(idItem, id, params) {
			this.itemBoxEdition.loading = true;
			this.itemBoxEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/box/${id}`,
				useToken: "access",
				body: params,
			});
			this.itemBoxs[idItem][id] = this.itemBoxEdition;
		},
		async deleteItemBox(idItem, id) {
			this.itemBoxEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/box/${id}`,
				useToken: "access",
			});
			delete this.itemBoxs[idItem][id];
			this.itemBoxEdition = {};
		},

		async getItemTagByInterval(idItem, limit = 100, offset = 0, expand = []) {
			this.itemTagsLoading = true;
			const tagsStore = useTagsStore();
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemTagList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const itemTag of newItemTagList["data"]) {
				this.itemTags[idItem][itemTag.id_tag] = itemTag;
				if (expand.includes("tag")) {
					tagsStore.tags[itemTag.id_tag] = itemTag["tag"];
				}
			}
			this.itemTagsTotalCount[idItem] = newItemTagList["count"];
			this.itemTagsLoading = false;
		},
		async getItemTagById(idItem, id, expand = []) {
			const tagsStore = useTagsStore();
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			if (!this.itemTags[idItem][id]) {
				this.itemTags[idItem][id] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemTags[idItem][id] = { ...this.itemTags[idItem][id], loading: true };
			this.itemTags[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/tag/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("tag")) {
				tagsStore.tags[this.itemTags[idItem][id].id_tag] = this.itemTags[idItem][id]["tag"];
			}
		},
		async createItemTag(idItem, params) {
			this.itemTagEdition.loading = true;
			this.itemTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/tag`,
				useToken: "access",
				body: params,
			});
			if (!this.itemTags[idItem]) {
				this.itemTags[idItem] = {};
			}
			this.itemTags[idItem][this.itemTagEdition.id_tag] = this.itemTagEdition;
		},
		async deleteItemTag(idItem, id) {
			this.itemTagEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/tag/${id}`,
				useToken: "access",
			});
			delete this.itemTags[idItem][id];
			this.itemTagEdition = {};
		},
		async createItemTagBulk(idItem, idList) {
			this.itemTagEdition.loading = true;
			this.itemTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/tag/bulk`,
				useToken: "access",
				body: idList,
			});
			if (!this.itemTagEdition[idItem]) {
				this.itemTagEdition[idItem] = {};
			}
			for (const itemTag of this.itemTagEdition["valide"]) {
				this.itemTags[idItem][itemTag.id_tag] = itemTag;
			}
		},
		async deleteItemTagBulk(idItem, idList) {
			this.itemTagEdition.loading = true;
			this.itemTagEdition = await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/tag/bulk`,
				useToken: "access",
				body: idList,
			});
			for (const itemTag of this.itemTagEdition["valide"]) {
				delete this.itemTags[idItem][itemTag.id_tag];
			}
		},

		async getItemCommandByInterval(idItem, limit = 100, offset = 0, expand = []) {
			this.itemCommandsLoading = true;
			const commandsStore = useCommandsStore();
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemCommandList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/command?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const itemCommand of newItemCommandList["data"]) {
				this.itemCommands[idItem][itemCommand.id_command] = itemCommand;
				if (expand.includes("command")) {
					commandsStore.commands[itemCommand.id_command] = itemCommand["command"];
				}
			}
			this.itemCommandsTotalCount[idItem] = newItemCommandList["count"];
			this.itemCommandsLoading = false;
		},
		async getItemCommandById(idItem, id, expand = []) {
			const commandsStore = useCommandsStore();
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			if (!this.itemCommands[idItem][id]) {
				this.itemCommands[idItem][id] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemCommands[idItem][id] = { ...this.itemCommands[idItem][id], loading: true };
			this.itemCommands[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/command/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("command")) {
				commandsStore.commands[this.itemCommands[idItem][id].id_command] = this.itemCommands[idItem][id]["command"];
			}
		},
		async createItemCommand(idItem, params) {
			this.itemCommandEdition.loading = true;
			this.itemCommandEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/command`,
				useToken: "access",
				body: params,
			});
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			this.itemCommands[idItem][this.itemCommandEdition.id_command] = this.itemCommandEdition;
		},
		async updateItemCommand(idItem, id, params) {
			this.itemCommandEdition.loading = true;
			this.itemCommandEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/command/${id}`,
				useToken: "access",
				body: params,
			});
			this.itemCommands[idItem][id] = this.itemCommandEdition;
		},
		async deleteItemCommand(idItem, id) {
			this.itemCommandEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/command/${id}`,
				useToken: "access",
			});
			delete this.itemCommands[idItem][id];
			this.itemCommandEdition = {};
		},
		async createItemCommandBulk(idItem, idList) {
			this.itemCommandEdition.loading = true;
			this.itemCommandEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/command/bulk`,
				useToken: "access",
				body: idList,
			});
			if (!this.itemCommands[idItem]) {
				this.itemCommands[idItem] = {};
			}
			for (const itemCommand of this.itemCommandEdition["valide"]) {
				this.itemCommands[idItem][itemCommand.id_command] = itemCommand;
			}
		},

		async getItemProjetByInterval(idItem, limit = 100, offset = 0, expand = []) {
			this.itemProjetsLoading = true;
			const projetsStore = useProjetsStore();
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemProjetList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/projet?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const itemProjet of newItemProjetList["data"]) {
				this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
				if (expand.includes("projet")) {
					projetsStore.projets[itemProjet.id_projet] = itemProjet["projet"];
				}
			}
			this.itemProjetsTotalCount[idItem] = newItemProjetList["count"];
			this.itemProjetsLoading = false;
		},
		async getItemProjetById(idItem, id, expand = []) {
			const projetsStore = useProjetsStore();
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			if (!this.itemProjets[idItem][id]) {
				this.itemProjets[idItem][id] = {};
			}
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.itemProjets[idItem][id] = { ...this.itemProjets[idItem][id], loading: true };
			this.itemProjets[idItem][id] = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/projet/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("projet")) {
				projetsStore.projets[this.itemProjets[idItem][id].id_projet] = this.itemProjets[idItem][id]["projet"];
			}
		},
		async createItemProjet(idItem, params) {
			this.itemProjetEdition.loading = true;
			this.itemProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/projet`,
				useToken: "access",
				body: params,
			});
			if (!this.itemProjets[idItem]) {
				this.itemProjets[idItem] = {};
			}
			this.itemProjets[idItem][this.itemProjetEdition.id_projet] = this.itemProjetEdition;
		},
		async updateItemProjet(idItem, id, params) {
			this.itemProjetEdition.loading = true;
			this.itemProjetEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/projet/${id}`,
				useToken: "access",
				body: params,
			});
			this.itemProjets[idItem][id] = this.itemProjetEdition;
		},
		async deleteItemProjet(idItem, id) {
			this.itemProjetEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${idItem}/projet/${id}`,
				useToken: "access",
			});
			delete this.itemProjets[idItem][id];
			this.itemProjetEdition = {};
		},
		async createItemProjetBulk(idItem, idList) {
			this.itemProjetEdition.loading = true;
			this.itemProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/projet/bulk`,
				useToken: "access",
				body: idList,
			});
			if (!this.itemProjetEdition[idItem]) {
				this.itemProjetEdition[idItem] = {};
			}
			for (const itemProjet of this.itemProjetEdition["valide"]) {
				this.itemProjets[idItem][itemProjet.id_projet] = itemProjet;
			}
		},

		async getImageByInterval(idItem, limit = 100, offset = 0, loadThumbnails = true, loadImages = false) {
			this.imagesLoading = true;
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			let newImagesList = await fetchWrapper.get({
				url: `${baseUrl}/item/${idItem}/img?limit=${limit}&offset=${offset}`,
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
			this.imagesTotalCount[idItem] = newImagesList["count"];
			this.imagesLoading = false;
		},
		async getImageById(idItem, id, loadThumbnails = true, loadImages = false) {
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			if (!this.images[idItem][id]) {
				this.images[idItem][id] = {};
			}
			this.images[idItem][id] = { ...this.images[idItem][id], loading: true };
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
			this.imageEdition.loading = true;
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			const formData = new FormData();
			formData.append("nom_img", params.nom_img);
			formData.append("description_img", params.description_img);
			formData.append("img_file", params.image);
			this.imageEdition = await fetchWrapper.post({
				url: `${baseUrl}/item/${idItem}/img`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.images[idItem][this.imageEdition.id_img] = this.imageEdition;
			if (loadImages && !this.imagesURL[this.imageEdition.id_img]) {
				await this.showImageById(idItem, this.imageEdition.id_img);
			}
			if (loadThumbnails && !this.thumbnailsURL[this.imageEdition.id_img]) {
				await this.showThumbnailById(idItem, this.imageEdition.id_img);
			}
		},
		async updateImage(idItem, id_img, params, loadThumbnails = true, loadImages = false) {
			this.imageEdition.loading = true;
			if (!this.images[idItem]) {
				this.images[idItem] = {};
			}
			this.imageEdition = await fetchWrapper.put({
				url: `${baseUrl}/item/${idItem}/img/${id_img}`,
				useToken: "access",
				body: params,
			});
			this.images[idItem][id_img] = this.imageEdition;
			if (loadImages && !this.imagesURL[this.imageEdition.id_img]) {
				await this.showImageById(idItem, this.imageEdition.id_img);
			}
			if (loadThumbnails && !this.thumbnailsURL[this.imageEdition.id_img]) {
				await this.showThumbnailById(idItem, this.imageEdition.id_img);
			}
		},
		async deleteImage(iditem, id_img) {
			this.imageEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/item/${iditem}/img/${id_img}`,
				useToken: "access",
			});
			delete this.images[iditem][id_img];
			this.imageEdition = {};
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
