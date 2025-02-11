import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useUsersStore, useItemsStore } from "@/stores";

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
	}),
	actions: {
		async getProjetByInterval(limit = 100, offset = 0, expand = []) {
			this.projetsLoading = true;
			const expandString = expand.join(",");
			let newProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet?limit=${limit}&offset=${offset}&expand=${expandString}`,
				useToken: "access",
			});
			for (const projet of newProjetList["data"]) {
				this.projets[projet.id_projet] = projet;
				this.commentairesTotalCount[projet.id_projet] = projet.projets_commentaires_count;
				this.documentsTotalCount[projet.id_projet] = projet.projets_documents_count;
				this.itemsTotalCount[projet.id_projet] = projet.projets_items_count;
				if (expand.indexOf("projets_commentaires") > -1) {
					this.commentaires[projet.id_projet] = {};
					for (const commentaire of projet.projets_commentaires) {
						this.commentaires[projet.id_projet][commentaire.id_projet_commentaire] = commentaire;
					}
				}
				if (expand.indexOf("projets_documents") > -1) {
					this.documents[projet.id_projet] = {};
					for (const document of projet.projets_documents) {
						this.documents[projet.id_projet][document.id_projet_document] = document;
					}
				}
				if (expand.indexOf("projets_items") > -1) {
					this.items[projet.id_projet] = {};
					for (const item of projet.projets_items) {
						this.items[projet.id_projet][item.id_item] = item;
					}
				}
			}
			this.projetsTotalCount = newProjetList["count"];
			this.projetsLoading = false;
		},
		async getProjetById(id, expand = []) {
			this.projets[id] = { loading: true };
			const expandString = expand.join(",");
			this.projets[id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${id}?expand=${expandString}`,
				useToken: "access",
			});
			this.commentairesTotalCount[id] = this.projets[id].projets_commentaires_count;
			this.documentsTotalCount[id] = this.projets[id].projets_documents_count;
			this.itemsTotalCount[id] = this.projets[id].projets_items_count;
			if (expand.indexOf("projets_commentaires") > -1) {
				this.commentaires[id] = {};
				for (const commentaire of this.projets[id].projets_commentaires) {
					this.commentaires[id][commentaire.id_projet_commentaire] = commentaire;
				}
			}
			if (expand.indexOf("projets_documents") > -1) {
				this.documents[id] = {};
				for (const document of this.projets[id].projets_documents) {
					this.documents[id][document.id_projet_document] = document;
				}
			}
			if (expand.indexOf("projets_items") > -1) {
				this.items[id] = {};
				for (const item of this.projets[id].projets_items) {
					this.items[id][item.id_item] = item;
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
			this.projets[id] = params;
		},
		async deleteProjet(id) {
			this.projetEdition.loading = true;
			this.projetEdition = await fetchWrapper.delete({
				url: `${baseUrl}/projet/${id}`,
				useToken: "access",
			});
			delete this.projets[id];
		},

		async getCommentaireByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			const userStore = useUsersStore();
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			this.commentairesLoading = true;
			const expandString = expand.join(",");
			let newCommentaireList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/commentaire?limit=${limit}&offset=${offset}&expand=${expandString}`,
				useToken: "access",
			});
			for (const commentaire of newCommentaireList["data"]) {
				this.commentaires[idProjet][commentaire.id_projet_commentaire] = commentaire;
				if (expand.indexOf("user") > -1) {
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
			this.commentaires[idProjet][id] = { loading: true };
			const expandString = expand.join(",");
			this.commentaires[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}?expand=${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("user") > -1) {
				userStore.users[this.commentaires[idProjet][id].id_user] = this.commentaires[idProjet][id].user;
			}
		},
		async createCommentaire(idProjet, params) {
			this.commentaireEdition = { loading: true };
			this.commentaireEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet/${idProjet}/commentaire`,
				useToken: "access",
				body: params,
			});
			if (!this.commentaires[idProjet]) {
				this.commentaires[idProjet] = {};
			}
			this.commentaires[idProjet][this.commentaireEdition.id_projet_commentaire] = this.commentaireEdition;
		},
		async updateCommentaire(idProjet, id, params) {
			this.commentaireEdition = { loading: true };
			this.commentaireEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
				body: params,
			});
			this.commentaires[idProjet][id] = params;
		},
		async deleteCommentaire(idProjet, id) {
			this.commentaireEdition = { loading: true };
			this.commentaireEdition = await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/commentaire/${id}`,
				useToken: "access",
			});
			delete this.commentaires[idProjet][id];
		},

		async getDocumentByList(idProjet, idResearch = [], expand = []) {
			// init list if not exist
			if (!this.documents[idProjet]) {
				this.documents[idProjet] = {};
			}
			// query
			this.documentsLoading = true;
			const idResearchString = idResearch.join(",");
			const expandString = expand.join(",");
			let newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document?&idResearch=${idResearchString}&expand=${expandString}`,
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
			const expandString = expand.join(",");
			let newDocumentList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document?limit=${limit}&offset=${offset}&expand=${expandString}`,
				useToken: "access",
			});
			for (const document of newDocumentList["data"]) {
				this.documents[idProjet][document.id_projet_document] = document;
			}
			this.documentsTotalCount[idProjet] = newDocumentList["count"];
			this.documentsLoading = false;
		},
		async getDocumentById(idProjet, id) {
			this.documents[idProjet][id] = { loading: true };
			this.documents[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
			});
		},
		async createDocument(idProjet, params) {
			this.documentEdition = { loading: true };
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
			this.documentEdition = { loading: true };
			const formData = new FormData();
			formData.append("name_projet_document", params.name_projet_document);
			formData.append("document", params.document);
			this.documentEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
			this.documents[idProjet][id] = params;
		},
		async deleteDocument(idProjet, id) {
			this.documentEdition = { loading: true };
			this.documentEdition = await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/document/${id}`,
				useToken: "access",
			});
			delete this.documents[idProjet][id];
		},
		async downloadDocument(idProjet, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/projet/${idProjet}/document/${id}/download`,
				useToken: "access",
			});
		},

		async getItemByList(idProjet, idResearch = [], expand = []) {
			const itemStore = useItemsStore();
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			// query
			this.itemsLoading = true;
			const idResearchString = idResearch.join(",");
			const expandString = expand.join(",");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item?&idResearch=${idResearchString}&expand=${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idProjet][item.id_item] = item;
				if (expand.indexOf("item") > -1) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idProjet] = newItemList["count"];
			this.itemsLoading = false;
		},
		async getItemByInterval(idProjet, limit = 100, offset = 0, expand = []) {
			const itemStore = useItemsStore();
			if (!this.items[idProjet]) {
				this.items[idProjet] = {};
			}
			// query
			this.itemsLoading = true;
			const expandString = expand.join(",");
			let newItemList = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item?limit=${limit}&offset=${offset}&expand=${expandString}`,
				useToken: "access",
			});
			for (const item of newItemList["data"]) {
				this.items[idProjet][item.id_item] = item;
				if (expand.indexOf("item") > -1) {
					itemStore.items[item.id_item] = item.item;
				}
			}
			this.itemsTotalCount[idProjet] = newItemList["count"];
			this.itemsLoading = false;
		},
		async getItemById(idProjet, id, expand = []) {
			const itemStore = useItemsStore();
			this.items[idProjet][id] = { loading: true };
			const expandString = expand.join(",");
			this.items[idProjet][id] = await fetchWrapper.get({
				url: `${baseUrl}/projet/${idProjet}/item/${id}&expand=${expandString}`,
				useToken: "access",
			});
			if (expand.indexOf("item") > -1) {
				itemStore.items[id] = this.items[idProjet][id].item;
			}
		},
		async createItem(idProjet, params) {
			this.itemEdition = { loading: true };
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
			this.itemEdition = { loading: true };
			this.itemEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
				body: params,
			});
			this.items[idProjet][id] = params;
		},
		async deleteItem(idProjet, id) {
			this.itemEdition = { loading: true };
			this.itemEdition = await fetchWrapper.delete({
				url: `${baseUrl}/projet/${idProjet}/item/${id}`,
				useToken: "access",
			});
			delete this.items[idProjet][id];
		},
		async createItemBulk(idProjet, params) {
			this.itemEdition = { loading: true };
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
	},
});
