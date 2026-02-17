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

		commentairesTotalCount: {},
		commentairesLoading: false,
		commentaires: {},
		commentaireEdition: {},

		documentsTotalCount: {},
		documentsLoading: false,
		documents: {},
		documentEdition: {},

		itemsTotalCount: {},
		itemsLoading: false,
		items: {},
		itemEdition: {},
	}),
	actions: {
		async getCommandByList(idResearch = [], expand = []) {
			this.commandsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const newCommandList = await fetchWrapper.get({
				url: `${baseUrl}/command?${idResearchString}&${expandString}`,
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
			this.commandsTotalCount = newCommandList["count"];
			this.commandsLoading = false;
		},
		async getCommandByInterval(limit = 100, offset = 0, expand = []) {
			this.commandsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const newCommandList = await fetchWrapper.get({
				url: `${baseUrl}/command?limit=${limit}&offset=${offset}&${expandString}`,
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
			this.commandsTotalCount = newCommandList["count"];
			this.commandsLoading = false;
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

		async getCommentaireByInterval(idCommand, limit = 100, offset = 0, expand = []) {
			if (!this.commentaires[idCommand]) {
				this.commentaires[idCommand] = {};
			}
			this.commentairesLoading = true;
			const userStore = useUsersStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const newCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/commentaire?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const commentaire of newCommentaireList["data"]) {
				this.commentaires[idCommand][commentaire.id_command_commentaire] = commentaire;
				if (expand.includes("user")) {
					userStore.users[commentaire.id_user] = commentaire.user;
				}
			}
			this.commentairesTotalCount[idCommand] = newCommentaireList["count"];
			this.commentairesLoading = false;
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

		async getDocumentByInterval(idCommand, limit = 100, offset = 0, expand = []) {
			if (!this.documents[idCommand]) {
				this.documents[idCommand] = {};
			}
			this.documentsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/document?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idCommand][document.id_command_document] = document;
			}
			this.documentsTotalCount[idCommand] = newDocumentList["count"];
			this.documentsLoading = false;
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

		async getItemByInterval(idCommand, limit = 100, offset = 0, expand = []) {
			if (!this.items[idCommand]) {
				this.items[idCommand] = {};
			}
			this.itemsLoading = true;
			const itemStore = useItemsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/command/${idCommand}/item?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idCommand][item.id_item] = item;
				if (expand.includes("item")) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idCommand] = newItemList["count"];
			this.itemsLoading = false;
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
	},
});
