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
		async getProjetByList(idResearch = [], expand = []) {
			this.projetsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].filter((str) => str).join("&");
			const newProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet?${paramString}`,
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
						this.statusHistory[projet.id_projet][statusHistory.id_projet_status] = statusHistory;
					}
				}
			}
			this.projetsLoading = false;
		},
		async getProjetByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.projetsLoading = true;
			if (clear) {
				this.projets = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet?${paramString}`,
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
						this.statusHistory[projet.id_projet][statusHistory.id_projet_status] = statusHistory;
					}
				}
			}
			this.projetsTotalCount = newProjetList["pagination"]?.["total"] || 0;
			this.projetsLoading = false;
			return [newProjetList["pagination"]?.["nextOffset"] || 0, newProjetList["pagination"]?.["hasMore"] || false];
		},
		async getProjetById(id, expand = []) {
			if (!this.projets[id]) {
				this.projets[id] = {};
			}
			this.projets[id].loading = true;
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
					this.statusHistory[id][statusHistory.id_projet_status] = statusHistory;
				}
			}
		},
		async createProjet(params) {
			const projet = await fetchWrapper.post({
				url: `${baseUrl}/projet`,
				useToken: "access",
				body: params,
			});
			this.projets[projet.id_projet] = projet;
		},
		async updateProjet(id, params) {
			this.projets[id] = await fetchWrapper.put({
				url: `${baseUrl}/projet/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteProjet(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${id}`,
				useToken: "access",
			});
			delete this.projets[id];
		},

		async getCommentaireByInterval(idProjet, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.commentaires[idProjet] || clear) {
				this.commentaires[idProjet] = {};
			}
			this.commentairesLoading = true;
			const userStore = useUsersStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/commentaire?${paramString}`,
				useToken: "access",
			});
			for (const commentaire of newCommentaireList["data"]) {
				this.commentaires[idProjet][commentaire.id_projet_commentaire] = commentaire;
				if (expand.includes("user")) {
					userStore.users[commentaire.id_user] = commentaire.user;
				}
			}
			this.commentairesTotalCount[idProjet] = newCommentaireList["pagination"]?.["total"] || 0;
			this.commentairesLoading = false;
			return [newCommentaireList["pagination"]?.["nextOffset"] || 0, newCommentaireList["pagination"]?.["hasMore"] || false];
		},
		async getCommentaireById(idProjet, id, expand = []) {
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			if (!this.commentaires[idProjet][id]) {
				this.commentaires[idProjet][id] = {};
			}
			this.commentaires[idProjet][id].loading = true;
			const userStore = useUsersStore();
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
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			const commentaire = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/commentaire`,
				useToken: "access",
				body: params,
			});
			this.commentaires[idProjet][commentaire.id_projet_commentaire] = commentaire;
			this.commentairesTotalCount[idProjet] += 1;
		},
		async updateCommentaire(idProjet, id, params) {
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			this.commentaires[idProjet][id] = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCommentaire(idProjet, id) {
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
			});
			delete this.commentaires[idProjet][id];
		},

		async getDocumentByInterval(idProjet, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.documents[idProjet] || clear) {
				this.documents[idProjet] = {};
			}
			this.documentsLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document?${paramString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idProjet][document.id_projet_document] = document;
			}
			this.documentsTotalCount[idProjet] = newDocumentList["pagination"]?.["total"] || 0;
			this.documentsLoading = false;
			return [newDocumentList["pagination"]?.["nextOffset"] || 0, newDocumentList["pagination"]?.["hasMore"] || false];
		},
		async getDocumentById(idProjet, id) {
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			if (!this.documents[idProjet][id]) {
				this.documents[idProjet][id] = {};
			}
			this.documents[idProjet][id].loading = true;
			this.documents[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
			});
		},
		async createDocument(idProjet, params) {
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			const formData = new FormData();
			formData.append("name_projet_document", params.name_projet_document);
			formData.append("document", params.document);
			const document = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/document`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.documents[idProjet][document.id_projet_document] = document;
		},
		async updateDocument(idProjet, id, params) {
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			this.documents[idProjet][id] = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteDocument(idProjet, id) {
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
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

		async getItemByInterval(idProjet, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.items[idProjet] || clear) {
				this.items[idProjet] = {};
			}
			this.itemsLoading = true;
			const itemStore = useItemsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newItemList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item?${paramString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idProjet][item.id_item] = item;
				if (expand.includes("item")) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idProjet] = newItemList["pagination"]?.["total"] || 0;
			this.itemsLoading = false;
			return [newItemList["pagination"]?.["nextOffset"] || 0, newItemList["pagination"]?.["hasMore"] || false];
		},
		async getItemById(idProjet, id, expand = []) {
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			if (!this.items[idProjet][id]) {
				this.items[idProjet][id] = {};
			}
			this.items[idProjet][id].loading = true;
			const itemStore = useItemsStore();
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
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			const item = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/item`,
				useToken: "access",
				body: params,
			});
			this.items[idProjet][item.id_item] = item;
		},
		async updateItem(idProjet, id, params) {
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			this.items[idProjet][id] = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteItem(idProjet, id) {
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
			});
			delete this.items[idProjet][id];
		},
		async createItemBulk(idProjet, params) {
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			const itemBulk = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/item/bulk`,
				useToken: "access",
				body: params,
			});
			for (const item of itemBulk["valide"]) {
				this.items[idProjet][item.id_item] = item;
			}
		},

		async getProjetTagProjetByInterval(idProjet, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.projetTagProjet[idProjet] || clear) {
				this.projetTagProjet[idProjet] = {};
			}
			this.projetTagProjetLoading = true;
			const projetTagStore = useProjetTagsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newProjetTagProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/projet-tag?${paramString}`,
				useToken: "access",
			});
			for (const projetTagProjet of newProjetTagProjetList["data"]) {
				this.projetTagProjet[idProjet][projetTagProjet.id_projet_tag] = projetTagProjet;
				if (expand.includes("projet_tag")) {
					projetTagStore.projetTags[projetTagProjet.id_projet_tag] = projetTagProjet.projet_tag;
				}
			}
			this.projetTagProjetTotalCount[idProjet] = newProjetTagProjetList["pagination"]?.["total"] || 0;
			this.projetTagProjetLoading = false;
			return [newProjetTagProjetList["pagination"]?.["nextOffset"] || 0, newProjetTagProjetList["pagination"]?.["hasMore"] || false];
		},
		async getProjetTagProjetById(idProjet, idProjetTag, expand = []) {
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			if (!this.projetTagProjet[idProjet][idProjetTag]) {
				this.projetTagProjet[idProjet][idProjetTag] = {};
			}
			this.projetTagProjet[idProjet][idProjetTag].loading = true;
			const projetTagStore = useProjetTagsStore();
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
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			const projetTag = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/projet-tag`,
				useToken: "access",
				body: params,
			});
			this.projetTagProjet[idProjet][projetTag.id_projet_tag] = projetTag;
		},
		async deleteProjetTagProjet(idProjet, idProjetTag) {
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/${idProjetTag}`,
				useToken: "access",
			});
			delete this.projetTagProjet[idProjet][idProjetTag];
		},
		async createProjetTagProjetBulk(idProjet, params) {
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			const projetTagBulk = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTagProjet of projetTagBulk["valide"]) {
				this.projetTagProjet[idProjet][projetTagProjet.id_projet_tag] = projetTagProjet;
			}
		},
		async deleteProjetTagProjetBulk(idProjet, params) {
			if (!this.projetTagProjet[idProjet]) {
				this.projetTagProjet[idProjet] = {};
			}
			const projetTagBulk = await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const idProjetTag of projetTagBulk["valide"]) {
				delete this.projetTagProjet[idProjet][idProjetTag];
			}
		},

		async getStatusHistoryByInterval(idProjet, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.statusHistory[idProjet] || clear) {
				this.statusHistory[idProjet] = {};
			}
			this.statusHistoryLoading = true;
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newStatusHistoryList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/status-history?${paramString}`,
				useToken: "access",
			});
			for (const statusHistory of newStatusHistoryList["data"]) {
				this.statusHistory[idProjet][statusHistory.id_projet_status] = statusHistory;
			}
			this.statusHistoryTotalCount[idProjet] = newStatusHistoryList["pagination"]?.["total"] || 0;
			this.statusHistoryLoading = false;
			return [newStatusHistoryList["pagination"]?.["nextOffset"] || 0, newStatusHistoryList["pagination"]?.["hasMore"] || false];
		},
		async getStatusHistoryById(idProjet, id, expand = []) {
			if (!this.statusHistory[idProjet]) {
				this.statusHistory[idProjet] = {};
			}
			if (!this.statusHistory[idProjet][id]) {
				this.statusHistory[idProjet][id] = {};
			}
			this.statusHistory[idProjet][id].loading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.statusHistory[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/status-history/${id}?${expandString}`,
				useToken: "access",
			});
		},
	},
});
