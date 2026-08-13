import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useUsersStore, useItemsStore, useProjetTagsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	projets_commentaires: (store, idProjet, data) => {
		store.commentaires[idProjet] = {};
		for (const commentaire of data) {
			store.commentaires[idProjet][commentaire.id_projet_commentaire] = commentaire;
		}
	},
	projets_documents: (store, idProjet, data) => {
		store.documents[idProjet] = {};
		for (const document of data) {
			store.documents[idProjet][document.id_projet_document] = document;
		}
	},
	projets_items: (store, idProjet, data) => {
		store.items[idProjet] = {};
		for (const item of data) {
			store.items[idProjet][item.id_item] = item;
		}
	},
	projets_projet_tags: (store, idProjet, data) => {
		store.projetTagProjet[idProjet] = {};
		for (const projetTagProjet of data) {
			store.projetTagProjet[idProjet][projetTagProjet.id_projet_tag] = projetTagProjet;
		}
	},
	projets_status_history: (store, idProjet, data) => {
		store.statusHistory[idProjet] = {};
		for (const statusHistory of data) {
			store.statusHistory[idProjet][statusHistory.id_projet_status] = statusHistory;
		}
	},
};

function hydrateProjet(store, idProjet, projet, expand = []) {
	store.commentairesTotalCount[idProjet] = projet.projets_commentaires_count;
	store.documentsTotalCount[idProjet] = projet.projets_documents_count;
	store.itemsTotalCount[idProjet] = projet.projets_items_count;
	store.projetTagProjetTotalCount[idProjet] = projet.projets_tags_count;
	store.statusHistoryTotalCount[idProjet] = projet.projets_status_history_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idProjet, projet[key]);
		}
	}
}

const projetResource = createMainResource({
	path: () => "/projet",
	idField: "id_projet",
	stateKey: "projets",
	countKey: "projetsTotalCount",
	loadingKey: "projetsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateProjet(store, entity.id_projet, entity, expand);
	},
});

const commentaireResource = createNestedResource({
	path: (idProjet) => `/projet/${idProjet}/commentaire`,
	idField: "id_projet_commentaire",
	stateKey: "commentaires",
	countKey: "commentairesTotalCount",
	loadingKey: "commentairesLoading",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("user")) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});
const documentResource = createNestedResource({
	path: (idProjet) => `/projet/${idProjet}/document`,
	idField: "id_projet_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
});
const itemResource = createNestedResource({
	path: (idProjet) => `/projet/${idProjet}/item`,
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("item")) {
			const itemsStore = useItemsStore();
			itemsStore.items[entity.id_item] = entity.item;
		}
	},
});
const projetTagProjetResource = createNestedResource({
	path: (idProjet) => `/projet/${idProjet}/projet_tag`,
	idField: "id_projet_tag",
	stateKey: "projetTagProjet",
	countKey: "projetTagProjetTotalCount",
	loadingKey: "projetTagProjetLoading",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("projet_tag")) {
			const projetTagsStore = useProjetTagsStore();
			projetTagsStore.projetTags[entity.id_projet_tag] = entity.projet_tag;
		}
	},
});
const statusHistoryResource = createNestedResource({
	path: (idProjet) => `/projet/${idProjet}/status-history`,
	idField: "id_projet_status",
	stateKey: "statusHistory",
	countKey: "statusHistoryTotalCount",
	loadingKey: "statusHistoryLoading",
});

export const useProjetsStore = defineStore("projets",{
	state: () => ({
		projetsLoading: false,
		projetsTotalCount: 0,
		projets: {},
		projetEdition: {},

		commentairesLoading: false,
		commentairesTotalCount: {},
		commentaires: {},
		commentaireEdition: {},

		documentsLoading: false,
		documentsTotalCount: {},
		documents: {},
		documentEdition: {},

		itemsLoading: false,
		itemsTotalCount: {},
		items: {},
		itemEdition: {},

		projetTagProjetLoading: false,
		projetTagProjetTotalCount: {},
		projetTagProjet: {},
		projetTagProjetEdition: {},

		statusHistoryTotalCount: {},
		statusHistoryLoading: false,
		statusHistory: {},
	}),
	actions: {
		getProjetByList: projetResource.getByList,
		getProjetByInterval: projetResource.getByInterval,
		getProjetById: projetResource.getById,
		createProjet: projetResource.create,
		updateProjet: projetResource.update,
		deleteProjet: projetResource.remove,
		loadToEdition(id, preset = null) {
			this.projetEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.projetEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.projets[id]) {
				this.projetEdition[id] = {
					loading: false,
					nom_projet: this.projets[id].nom_projet,
					description_projet: this.projets[id].description_projet,
					url_projet: this.projets[id].url_projet,
					status_projet: this.projets[id].status_projet,
					date_debut_projet: this.projets[id].date_debut_projet,
					date_fin_projet: this.projets[id].date_fin_projet,
				};
			} else {
				this.projetEdition[id] = {
					loading: false,
				};
			}
			this.commentaireEdition[id] = {};
			this.documentEdition[id] = {};
			this.itemEdition[id] = {};
			this.projetTagProjetEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.projetEdition[id]) {
				this.projetEdition[id] = {};
			}
			this.projetEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.projetEdition[id];
			delete this.commentaireEdition[id];
			delete this.documentEdition[id];
			delete this.itemEdition[id];
			delete this.projetTagProjetEdition[id];
		},

		getCommentaireByInterval: commentaireResource.getByInterval,
		getCommentaireById: commentaireResource.getById,
		createCommentaire: commentaireResource.create,
		updateCommentaire: commentaireResource.update,
		deleteCommentaire: commentaireResource.remove,

		getDocumentByInterval: documentResource.getByInterval,
		getDocumentById: documentResource.getById,
		createDocument: documentResource.create,
		updateDocument: documentResource.update,
		deleteDocument: documentResource.remove,
		async downloadDocument(idProjet, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/projet/${idProjet}/document/${id}/download`,
				useToken: "access",
			});
		},
		
		getItemByInterval: itemResource.getByInterval,
		getItemById: itemResource.getById,
		createItem: itemResource.create,
		updateItem: itemResource.update,
		deleteItem: itemResource.remove,
		createItemBulk: itemResource.createBulk,
		
		getProjetTagProjetByInterval: projetTagProjetResource.getByInterval,
		getProjetTagProjetById: projetTagProjetResource.getById,
		createProjetTagProjet: projetTagProjetResource.create,
		deleteProjetTagProjet: projetTagProjetResource.remove,
		createProjetTagProjetBulk: projetTagProjetResource.createBulk,
		deleteProjetTagProjetBulk: projetTagProjetResource.removeBulk,

		getStatusHistoryByInterval: statusHistoryResource.getByInterval,
		getStatusHistoryById: statusHistoryResource.getById,
	},
});
