import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useUsersStore, useItemsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCommandsStore = defineStore("commands",{
	state: () => ({
		commandsLoading: true,
		commandsTotalCount: 0,
		commands: {},
		commandEdition: {},
		commandReady: {},

		commentairesTotalCount: {},
		commentairesLoading: false,
		commentaires: {},
		commentaireEdition: {},
		commentaireReady: {},

		documentsTotalCount: {},
		documentsLoading: false,
		documents: {},
		documentEdition: {},
		documentReady: {},

		itemsTotalCount: {},
		itemsLoading: false,
		items: {},
		itemEdition: {},
		itemReady: {},
	}),
	actions: {
		async getCommandByList(idResearch = [], expand = []) {
			this.commandsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newCommandList = await fetchWrapper.get({
				url: `${baseUrl}/command?${paramString}`,
				useToken: "access",
			});
			for (const command of newCommandList["data"]) {
				this.commands[command.id_command] = command;
				this.commentairesTotalCount[command.id_command] = command.commands_commentaires_count;
				this.documentsTotalCount[command.id_command] = command.commands_documents_count;
				this.itemsTotalCount[command.id_command] = command.commands_items_count;
				if (expand.includes("commands_commentaires")) {
					this.commentaires[command.id_command] = {};
					for (const commentaire of command.commands_commentaires) {
						this.commentaires[command.id_command][commentaire.id_command_commentaire] = commentaire;
					}
				}
				if (expand.includes("commands_documents")) {
					this.documents[command.id_command] = {};
					for (const document of command.commands_documents) {
						this.documents[command.id_command][document.id_command_document] = document;
					}
				}
				if (expand.includes("commands_items")) {
					this.items[command.id_command] = {};
					for (const item of command.commands_items) {
						this.items[command.id_command][item.id_item] = item;
					}
				}
			}
			this.commandsLoading = false;
		},
		async getCommandByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.commandsLoading = true;
			if (clear) {
				this.commands = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newCommandList = await fetchWrapper.get({
				url: `${baseUrl}/command?${paramString}`,
				useToken: "access",
			});
			for (const command of newCommandList["data"]) {
				this.commands[command.id_command] = command;
				this.commentairesTotalCount[command.id_command] = command.commands_commentaires_count;
				this.documentsTotalCount[command.id_command] = command.commands_documents_count;
				this.itemsTotalCount[command.id_command] = command.commands_items_count;
				if (expand.includes("commands_commentaires")) {
					this.commentaires[command.id_command] = {};
					for (const commentaire of command.commands_commentaires) {
						this.commentaires[command.id_command][commentaire.id_command_commentaire] = commentaire;
					}
				}
				if (expand.includes("commands_documents")) {
					this.documents[command.id_command] = {};
					for (const document of command.commands_documents) {
						this.documents[command.id_command][document.id_command_document] = document;
					}
				}
				if (expand.includes("commands_items")) {
					this.items[command.id_command] = {};
					for (const item of command.commands_items) {
						this.items[command.id_command][item.id_item] = item;
					}
				}
			}
			this.commandsTotalCount = newCommandList["pagination"]?.["total"] || 0;
			this.commandsLoading = false;
			return [newCommandList["pagination"]?.["nextOffset"] || 0, newCommandList["pagination"]?.["hasMore"] || false];
		},
		async getCommandById(id, expand = []) {
			if (!this.commands[id]) {
				this.commands[id] = {};
			}
			this.commands[id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.commands[id] = await fetchWrapper.get({
				url: `${baseUrl}/command/${id}?${expandString}`,
				useToken: "access",
			});
			this.commentairesTotalCount[id] = this.commands[id].commands_commentaires_count;
			this.documentsTotalCount[id] = this.commands[id].commands_documents_count;
			this.itemsTotalCount[id] = this.commands[id].commands_items_count;
			if (expand.includes("commands_commentaires")) {
				this.commentaires[id] = {};
				for (const commentaire of this.commands[id].commands_commentaires) {
					this.commentaires[id][commentaire.id_command_commentaire] = commentaire;
				}
			}
			if (expand.includes("commands_documents")) {
				this.documents[id] = {};
				for (const document of this.commands[id].commands_documents) {
					this.documents[id][document.id_command_document] = document;
				}
			}
			if (expand.includes("commands_items")) {
				this.items[id] = {};
				for (const item of this.commands[id].commands_items) {
					this.items[id][item.id_item] = item;
				}
			}
		},
		async createCommand(params) {
			const command = await fetchWrapper.post({
				url: `${baseUrl}/command`,
				useToken: "access",
				body: params,
			});
			this.commands[command.id_command] = command;
			return command.id_command;
		},
		async updateCommand(id, params) {
			this.commands[id] = await fetchWrapper.put({
				url: `${baseUrl}/command/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCommand(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/command/${id}`,
				useToken: "access",
			});
			delete this.commands[id];
		},
		async pushCommandReady() {
			for (const id in this.commandReady) {
				const change = this.commandReady[id];
				if (change.status === "new") {
					const newId = await this.createCommand(change.data);
					this.updateIdCommand(id, newId);
					this.pushDocumentReady(newId);
					this.pushItemReady(newId);
					delete this.commandReady[id];
				} else if (change.status === "modified") {
					await this.updateCommand(id, change.data);
					this.pushDocumentReady(id);
					this.pushItemReady(id);
					delete this.commandReady[id];
				} else if (change.status === "delete") {
					await this.deleteCommand(id);
					delete this.commandReady[id];
				}
			}
		},
		async pushCommandById(id) {
			if (!this.commandEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createCommand(this.commandEdition[id]);
				this.updateIdCommand(id, newId);
			} else {
				await this.updateCommand(id, this.commandEdition[id]);
			}
			this.pushDocumentReady(newId);
			this.pushItemReady(newId);
			return newId;
		},
		updateIdCommand(oldId, newId) {
			if (this.documentReady[oldId]) {
				this.documentReady[newId] = { ...this.documentReady[oldId], id_command: newId };
				delete this.documentReady[oldId];
			}
			if (this.documentEdition[oldId]) {
				this.documentEdition[newId] = { ...this.documentEdition[oldId], id_command: newId };
				delete this.documentEdition[oldId];
			}
			if (this.itemReady[oldId]) {
				this.itemReady[newId] = { ...this.itemReady[oldId], id_command: newId };
				delete this.itemReady[oldId];
			}
			if (this.itemEdition[oldId]) {
				this.itemEdition[newId] = { ...this.itemEdition[oldId], id_command: newId };
				delete this.itemEdition[oldId];
			}
		},
		commitCommandEdition(id, operation = "modified") { // return (sucess:bool, newStatus:string)
			if (!this.commandEdition[id]) {
				return { success: false, newStatus: null };
			}
			if (!this.commandReady[id]) {
				this.commandReady[id] = {};
			}
			if (this.commandReady[id].status === "new" && operation === "delete") {
				delete this.commandReady[id];
				return { success: true, newStatus: "delete" };
			} else if (this.commandReady[id].status === "modified" && operation === "delete") {
				this.commandReady[id].status = "delete";
			} else if (this.commandReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.commandReady[id].status = "modified";
			} else {
				this.commandReady[id].status = operation;
			}
			this.commandReady[id].data = { ...this.commandEdition[id] };
			return { success: true, newStatus: this.commandReady[id].status };
		},
		getAvailableEditionCommand() {
			// search existing "new{id}" in commandEdition to find available id for new COMMAND
			const newIds = Math.max(Object.keys(this.commandEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearCommandEdition() {
			this.commandEdition = {};
			this.commandReady = {};
		},
		clearCommandEditionById(id) {
			delete this.commandEdition[id];
			delete this.commandReady[id];
			this.clearDocumentEdition(id);
			this.clearItemEdition(id);
		},

		async getCommentaireByInterval(idCommand, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.commentaires[idCommand] || clear) {
				this.commentaires[idCommand] = {};
			}
			this.commentairesLoading = true;
			const userStore = useUsersStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/commentaire?${paramString}`,
				useToken: "access",
			});
			for (const commentaire of newCommentaireList["data"]) {
				this.commentaires[idCommand][commentaire.id_command_commentaire] = commentaire;
				if (expand.includes("user")) {
					userStore.users[commentaire.id_user] = commentaire.user;
				}
			}
			this.commentairesTotalCount[idCommand] = newCommentaireList["pagination"]?.["total"] || 0;
			this.commentairesLoading = false;
			return [newCommentaireList["pagination"]?.["nextOffset"] || 0, newCommentaireList["pagination"]?.["hasMore"] || false];
		},
		async getCommentaireById(idCommand, id, expand = []) {
			if (!this.commentaires[idCommand]) {
				this.commentaires[idCommand] = {};
			}
			if (!this.commentaires[idCommand][id]) {
				this.commentaires[idCommand][id] = {};
			}
			this.commentaires[idCommand][id].loading = true;
			const userStore = useUsersStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.commentaires[idCommand][id] = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/commentaire/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("user")) {
				userStore.users[this.commentaires[idCommand][id].id_user] = this.commentaires[idCommand][id].user;
			}
		},
		async createCommentaire(idCommand, params) {
			if (!this.commentaires[idCommand]) {
				this.commentaires[idCommand] = {};
			}
			const commentaire = await fetchWrapper.post({
				url: `${baseUrl}/command/${idCommand}/commentaire`,
				useToken: "access",
				body: params,
			});
			this.commentaires[idCommand][commentaire.id_command_commentaire] = commentaire;
		},
		async updateCommentaire(idCommand, id, params) {
			if (!this.commentaires[idCommand]) {
				this.commentaires[idCommand] = {};
			}
			this.commentaires[idCommand][id] = await fetchWrapper.put({
				url: `${baseUrl}/command/${idCommand}/commentaire/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCommentaire(idCommand, id) {
			if (!this.commentaires[idCommand]) {
				this.commentaires[idCommand] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/command/${idCommand}/commentaire/${id}`,
				useToken: "access",
			});
			delete this.commentaires[idCommand][id];
		},

		async getDocumentByInterval(idCommand, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.documents[idCommand] || clear) {
				this.documents[idCommand] = {};
			}
			this.documentsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/document?${paramString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idCommand][document.id_command_document] = document;
			}
			this.documentsTotalCount[idCommand] = newDocumentList["pagination"]?.["total"] || 0;
			this.documentsLoading = false;
			return [newDocumentList["pagination"]?.["nextOffset"] || 0, newDocumentList["pagination"]?.["hasMore"] || false];
		},
		async getDocumentById(idCommand, id, expand = []) {
			if (!this.documents[idCommand]) {
				this.documents[idCommand] = {};
			}
			if (!this.documents[idCommand][id]) {
				this.documents[idCommand][id] = {};
			}
			this.documents[idCommand][id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.documents[idCommand][id] = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/document/${id}?${expandString}`,
				useToken: "access",
			});
		},
		async createDocument(idCommand, params) {
			if (!this.documents[idCommand]) {
				this.documents[idCommand] = {};
			}
			const formData = new FormData();
			formData.append("name_command_document", params.name_command_document);
			formData.append("document", params.document);
			const document = await fetchWrapper.post({
				url: `${baseUrl}/command/${idCommand}/document`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.documents[idCommand][document.id_command_document] = document;
		},
		async updateDocument(idCommand, id, params) {
			if (!this.documents[idCommand]) {
				this.documents[idCommand] = {};
			}
			this.documents[idCommand][id] = await fetchWrapper.put({
				url: `${baseUrl}/command/${idCommand}/document/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteDocument(idCommand, id) {
			if (!this.documents[idCommand]) {
				this.documents[idCommand] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/command/${idCommand}/document/${id}`,
				useToken: "access",
			});
			delete this.documents[idCommand][id];
		},
		async downloadDocument(idCommand, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/command/${idCommand}/document/${id}/download`,
				useToken: "access",
			});
		},
		commitDocumentEdition(idCommand, id, operation = "modified") {
			if (!this.documentEdition[idCommand] || !this.documentEdition[idCommand][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.documentReady[idCommand]) {
				this.documentReady[idCommand] = {};
			}
			if (!this.documentReady[idCommand][id]) {
				this.documentReady[idCommand][id] = {};
			}
			if (this.documentReady[idCommand][id].status === "new" && operation === "delete") {
				delete this.documentReady[idCommand][id];
				return { success: true, newStatus: "delete" };
			} else if (this.documentReady[idCommand][id].status === "modified" && operation === "delete") {
				this.documentReady[idCommand][id].status = "delete";
			} else if (this.documentReady[idCommand][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.documentReady[idCommand][id].status = "modified";
			} else {
				this.documentReady[idCommand][id].status = operation;
			}
			this.documentReady[idCommand][id].data = { ...this.documentEdition[idCommand][id] };
			return { success: true, newStatus: this.documentReady[id].status };
		},
		async pushDocumentReady(idCommand) {
			if (!this.documentReady[idCommand]) {
				return;
			}
			for (const id in this.documentReady[idCommand]) {
				const change = this.documentReady[idCommand][id];
				if (change.status === "new") {
					await this.createDocument(idCommand, change.data);
					delete this.documentReady[id];
				} else if (change.status === "modified") {
					await this.updateDocument(idCommand, id, change.data);
					delete this.documentReady[id];
				} else if (change.status === "delete") {
					await this.deleteDocument(idCommand, id);
					delete this.documentReady[id];
				}
			}
		},
		clearDocumentEdition(idCommand) {
			this.documentEdition[idCommand] = {};
			this.documentReady[idCommand] = {};
		},

		async getItemByInterval(idCommand, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.items[idCommand] || clear) {
				this.items[idCommand] = {};
			}
			this.itemsLoading = true;
			const itemStore = useItemsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idCommand][item.id_item] = item;
				if (expand.includes("item")) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idCommand] = newItemList["pagination"]?.["total"] || 0;
			this.itemsLoading = false;
			return [newItemList["pagination"]?.["nextOffset"] || 0, newItemList["pagination"]?.["hasMore"] || false];
		},
		async getItemById(idCommand, id, expand = []) {
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			if (!this.items[idCommand][id]) {
				this.items[idCommand][id] = {};
			}
			this.items[idCommand][id].loading = true;
			const itemStore = useItemsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.items[idCommand][id] = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/item/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("item")) {
				itemStore.items[id] = this.items[idCommand][id].item;
			}
		},
		async createItem(idCommand, params) {
			const item = await fetchWrapper.post({
				url: `${baseUrl}/command/${idCommand}/item`,
				useToken: "access",
				body: params,
			});
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			this.items[idCommand][item.id_item] = item;
		},
		async updateItem(idCommand, id, params) {
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			this.items[idCommand][id] = await fetchWrapper.put({
				url: `${baseUrl}/command/${idCommand}/item/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteItem(idCommand, id) {
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/command/${idCommand}/item/${id}`,
				useToken: "access",
			});
			delete this.items[idCommand][id];
		},
		async createItemBulk(idCommand, params) {
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			const itemBulk = await fetchWrapper.post({
				url: `${baseUrl}/command/${idCommand}/item/bulk`,
				useToken: "access",
				body: params,
			});
			for (const item of itemBulk["valide"]) {
				this.items[idCommand][item.id_item] = item;
			}
		},
		commitItemEdition(idCommand, id, operation = "modified") {
			if (!this.itemEdition[idCommand] || !this.itemEdition[idCommand][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.itemReady[idCommand]) {
				this.itemReady[idCommand] = {};
			}
			if (!this.itemReady[idCommand][id]) {
				this.itemReady[idCommand][id] = {};
			}
			if (this.itemReady[idCommand][id].status === "new" && operation === "delete") {
				delete this.itemReady[idCommand][id];
				return { success: true, newStatus: "delete" };
			} else if (this.itemReady[idCommand][id].status === "modified" && operation === "delete") {
				this.itemReady[idCommand][id].status = "delete";
			} else if (this.itemReady[idCommand][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.itemReady[idCommand][id].status = "modified";
			} else {
				this.itemReady[idCommand][id].status = operation;
			}
			this.itemReady[idCommand][id].data = { ...this.itemEdition[idCommand][id] };
			return { success: true, newStatus: this.itemReady[id].status };
		},
		async pushItemReady(idCommand) {
			if (!this.itemReady[idCommand]) {
				return;
			}
			for (const id in this.itemReady[idCommand]) {
				const change = this.itemReady[idCommand][id];
				if (change.status === "new") {
					await this.createItem(idCommand, change.data);
					delete this.itemReady[id];
				} else if (change.status === "modified") {
					await this.updateItem(idCommand, id, change.data);
					delete this.itemReady[id];
				} else if (change.status === "delete") {
					await this.deleteItem(idCommand, id);
					delete this.itemReady[id];
				}
			}
		},
		clearItemEdition(idCommand) {
			this.itemEdition[idCommand] = {};
			this.itemReady[idCommand] = {};
		},
	},
});
