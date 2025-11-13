import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useUsersStore, useItemsStore, useProjetTagsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useProjetsStore = defineStore("projets",{
	state: () => ({
		projetsLoading: true,
		projetsTotalCount: 0,
		projets: {},
		projetEdition: {},

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

		projetTagProjetTotalCount: {},
		projetTagProjetLoading: true,
		projetTagProjet: {},
		projetTagProjetEdition: {},

		statusHistoryTotalCount: {},
		statusHistoryLoading: false,
		statusHistory: {},
	}),
	actions: {
		async getProjetByInterval(limit = 100, offset = 0, expand = []) {
			this.projetsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const projet of newProjetList["data"]) {
				this.projets[projet.id_projet] = projet;
				this.commentairesTotalCount[projet.id_projet] = projet.projets_commentaires_count;
				this.documentsTotalCount[projet.id_projet] = projet.projets_documents_count;
				this.itemsTotalCount[projet.id_projet] = projet.projets_items_count;
				this.projetTagProjetTotalCount[projet.id_projet] = projet.projets_tags_count;
				this.statusHistoryTotalCount[projet.id_projet] = projet.projets_status_history_count;
				if (expand.includes("projets_commentaires")) {
					this.commentaires[projet.id_projet] = {};
					for (const commentaire of projet.projets_commentaires) {
						this.commentaires[projet.id_projet][commentaire.id_projet_commentaire] = commentaire;
					}
				}
				if (expand.includes("projets_documents")) {
					this.documents[projet.id_projet] = {};
					for (const document of projet.projets_documents) {
						this.documents[projet.id_projet][document.id_projet_document] = document;
					}
				}
				if (expand.includes("projets_items")) {
					this.items[projet.id_projet] = {};
					for (const item of projet.projets_items) {
						this.items[projet.id_projet][item.id_item] = item;
					}
				}
				if (expand.includes("projets_projet_tags")) {
					this.projetTagProjet[projet.id_projet] = {};
					for (const projetTagProjet of projet.projets_projet_tags) {
						this.projetTagProjet[projet.id_projet][projetTagProjet.id_projet_tag] = projetTagProjet;
					}
				}
				if (expand.includes("projets_status_history")) {
					this.statusHistory[projet.id_projet] = {};
					for (const statusHistory of projet.projets_status_history) {
						this.statusHistory[projet.id_projet][statusHistory.id_status_history] = statusHistory;
					}
				}
			}
			this.projetsTotalCount = newProjetList["count"];
			this.projetsLoading = false;
		},
		async getProjetById(id, expand = []) {
			if (!this.projets[id]) {
				this.projets[id] = {};
			}
			this.projets[id] = { ...this.projets[id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projets[id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${id}?${expandString}`,
				useToken: "access",
			});
			this.commentairesTotalCount[id] = this.projets[id].projets_commentaires_count;
			this.documentsTotalCount[id] = this.projets[id].projets_documents_count;
			this.itemsTotalCount[id] = this.projets[id].projets_items_count;
			this.projetTagProjetTotalCount[id] = this.projets[id].projets_tags_count;
			this.statusHistoryTotalCount[id] = this.projets[id].projets_status_history_count;
			if (expand.includes("projets_commentaires")) {
				this.commentaires[id] = {};
				for (const commentaire of this.projets[id].projets_commentaires) {
					this.commentaires[id][commentaire.id_projet_commentaire] = commentaire;
				}
			}
			if (expand.includes("projets_documents")) {
				this.documents[id] = {};
				for (const document of this.projets[id].projets_documents) {
					this.documents[id][document.id_projet_document] = document;
				}
			}
			if (expand.includes("projets_items")) {
				this.items[id] = {};
				for (const item of this.projets[id].projets_items) {
					this.items[id][item.id_item] = item;
				}
			}
			if (expand.includes("projets_projet_tags")) {
				this.projetTagProjet[id] = {};
				for (const projetTagProjet of this.projets[id].projets_projet_tags) {
					this.projetTagProjet[id][projetTagProjet.id_projet_tag] = projetTagProjet;
				}
			}
			if (expand.includes("projets_status_history")) {
				this.statusHistory[id] = {};
				for (const statusHistory of this.projets[id].projets_status_history) {
					this.statusHistory[id][statusHistory.id_status_history] = statusHistory;
				}
			}
		},
		async createProjet(params) {
			this.projetEdition.loading = true;
			this.projetEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet`,
				useToken: "access",
				body: params,
			});
			this.projets[this.projetEdition.id_projet] = this.projetEdition;
		},
		async updateProjet(id, params) {
			this.projetEdition.loading = true;
			this.projetEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${id}`,
				useToken: "access",
				body: params,
			});
			this.projets[id] = this.projetEdition;
		},
		async deleteProjet(id) {
			this.projetEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${id}`,
				useToken: "access",
			});
			delete this.projets[id];
			this.projetEdition = {};
		},

		async getCommentaireByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			const userStore = useUsersStore();
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			this.commentairesLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/commentaire?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const commentaire of newCommentaireList["data"]) {
				this.commentaires[idProjet][commentaire.id_projet_commentaire] = commentaire;
				if (expand.includes("user")) {
					userStore.users[commentaire.id_user] = commentaire.user;
				}
			}
			this.commentairesTotalCount[idProjet] = newCommentaireList["count"];
			this.commentairesLoading = false;
		},
		async getCommentaireById(idProjet, id, expand = []) {
			const userStore = useUsersStore();
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			if (!this.commentaires[idProjet][id]) {
				this.commentaires[idProjet][id] = {};
			}
			this.commentaires[idProjet][id] = { ...this.commentaires[idProjet][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.commentaires[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}?${expandString}`,
				useToken: "access",
			});
			if (expand.includes("user")) {
				userStore.users[this.commentaires[idProjet][id].id_user] = this.commentaires[idProjet][id].user;
			}
		},
		async createCommentaire(idProjet, params) {
			this.commentaireEdition.loading = true;
			this.commentaireEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/commentaire`,
				useToken: "access",
				body: params,
			});
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			this.commentaires[idProjet][this.commentaireEdition.id_projet_commentaire] = this.commentaireEdition;
			this.commentairesTotalCount[idProjet] += 1;
		},
		async updateCommentaire(idProjet, id, params) {
			this.commentaireEdition.loading = true;
			this.commentaireEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
				body: params,
			});
			this.commentaires[idProjet][id] = this.commentaireEdition;
		},
		async deleteCommentaire(idProjet, id) {
			this.commentaireEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
			});
			delete this.commentaires[idProjet][id];
			this.commentaireEdition = {};
		},

		async getDocumentByList(idProjet, idResearch = [], expand = []) {
			// init list if not exist
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			// query
			this.documentsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idProjet][document.id_projet_document] = document;
			}
			this.documentsTotalCount[idProjet] = newDocumentList["count"];
			this.documentsLoading = false;
		},
		async getDocumentByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			// init list if not exist
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			// query
			this.documentsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idProjet][document.id_projet_document] = document;
			}
			this.documentsTotalCount[idProjet] = newDocumentList["count"];
			this.documentsLoading = false;
		},
		async getDocumentById(idProjet, id) {
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			if (!this.documents[idProjet][id]) {
				this.documents[idProjet][id] = {};
			}
			this.documents[idProjet][id] = { ...this.documents[idProjet][id], loading: true };
			this.documents[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
			});
		},
		async createDocument(idProjet, params) {
			this.documentEdition.loading = true;
			const formData = new FormData();
			formData.append("name_projet_document", params.name_projet_document);
			formData.append("document", params.document);
			this.documentEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/document`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			this.documents[idProjet][this.documentEdition.id_projet_document] = this.documentEdition;
		},
		async updateDocument(idProjet, id, params) {
			this.documents[idProjet][id].loading = true;
			this.documentEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
				body: params,
			});
			this.documents[idProjet][id] = this.documentEdition;
		},
		async deleteDocument(idProjet, id) {
			this.documentEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
			});
			delete this.documents[idProjet][id];
			this.documentEdition = {};
		},
		async downloadDocument(idProjet, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/projet/${idProjet}/document/${id}/download`,
				useToken: "access",
			});
		},

		async getItemByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			const itemStore = useItemsStore();
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			// query
			this.itemsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idProjet][item.id_item] = item;
				if (expand.includes("item")) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idProjet] = newItemList["count"];
			this.itemsLoading = false;
		},
		async getItemById(idProjet, id, expand = []) {
			const itemStore = useItemsStore();
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			if (!this.items[idProjet][id]) {
				this.items[idProjet][id] = {};
			}
			this.items[idProjet][id] = { ...this.items[idProjet][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.items[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item/${id}&${expandString}`,
				useToken: "access",
			});
			if (expand.includes("item")) {
				itemStore.items[id] = this.items[idProjet][id].item;
			}
		},
		async createItem(idProjet, params) {
			this.itemEdition.loading = true;
			this.itemEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/item`,
				useToken: "access",
				body: params,
			});
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			this.items[idProjet][this.itemEdition.id_item] = this.itemEdition;
		},
		async updateItem(idProjet, id, params) {
			this.itemEdition.loading = true;
			this.itemEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
				body: params,
			});
			this.items[idProjet][id] = this.itemEdition;
		},
		async deleteItem(idProjet, id) {
			this.itemEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
			});
			delete this.items[idProjet][id];
			this.itemEdition = {};
		},
		async createItemBulk(idProjet, params) {
			this.itemEdition.loading = true;
			this.itemEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/item/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			for (const item of this.itemEdition["valide"]) {
				this.items[idProjet][item.id_item] = item;
			}
		},

		async getProjetTagProjetByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			const projetTagStore = useProjetTagsStore();
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			this.projetTagProjetLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newProjetTagProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/projet-tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const projetTagProjet of newProjetTagProjetList["data"]) {
				this.projetTagProjet[idProjet][projetTagProjet.id_projet_tag] = projetTagProjet;
				if (expand.includes("projet_tag")) {
					projetTagStore.projetTags[projetTagProjet.id_projet_tag] = projetTagProjet.projet_tag;
				}
			}
			this.projetTagProjetTotalCount[idProjet] = newProjetTagProjetList["count"];
		},
		async getProjetTagProjetById(idProjet, idProjetTag, expand = []) {
			const projetTagStore = useProjetTagsStore();
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			if (!this.projetTagProjet[idProjet][idProjetTag]) {
				this.projetTagProjet[idProjet][idProjetTag] = {};
			}
			this.projetTagProjet[idProjet][idProjetTag] = { ...this.projetTagProjet[idProjet][idProjetTag], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projetTagProjet[idProjet][idProjetTag] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/${idProjetTag}&${expandString}`,
				useToken: "access",
			});
			if (expand.includes("projet_tag")) {
				projetTagStore.projetTags[idProjetTag] = this.projetTagProjet[idProjet][idProjetTag].projet_tag;
			}
		},
		async createProjetTagProjet(idProjet, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/projet-tag`,
				useToken: "access",
				body: params,
			});
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			this.projetTagProjet[idProjet][this.projetTagProjetEdition.id_projet_tag] = this.projetTagProjetEdition;
		},
		async deleteProjetTagProjet(idProjet, idProjetTag) {
			this.projetTagProjetEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/${idProjetTag}`,
				useToken: "access",
			});
			delete this.projetTagProjet[idProjet][idProjetTag];
			this.projetTagProjetEdition = {};
		},
		async createProjetTagProjetBulk(idProjet, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			for (const projetTagProjet of this.projetTagProjetEdition["valide"]) {
				this.projetTagProjet[idProjet][projetTagProjet.id_projet_tag] = projetTagProjet;
			}
		},
		async deleteProjetTagProjetBulk(idProjet, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const idProjetTag of this.projetTagProjetEdition["valide"]) {
				delete this.projetTagProjet[idProjet][idProjetTag];
			}
		},

		async getStatusHistoryByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			if (!this.statusHistory[idProjet]) {
				this.statusHistory[idProjet] = {};
			}
			this.statusHistoryLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newStatusHistoryList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/status-history?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const statusHistory of newStatusHistoryList["data"]) {
				this.statusHistory[idProjet][statusHistory.id_status_history] = statusHistory;
			}
			this.statusHistoryTotalCount[idProjet] = newStatusHistoryList["count"];
			this.statusHistoryLoading = false;
		},
		async getStatusHistoryById(idProjet, id, expand = []) {
			if (!this.statusHistory[idProjet]) {
				this.statusHistory[idProjet] = {};
			}
			if (!this.statusHistory[idProjet][id]) {
				this.statusHistory[idProjet][id] = {};
			}
			this.statusHistory[idProjet][id] = { ...this.statusHistory[idProjet][id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.statusHistory[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/status-history/${id}?${expandString}`,
				useToken: "access",
			});
		},
	},
});
