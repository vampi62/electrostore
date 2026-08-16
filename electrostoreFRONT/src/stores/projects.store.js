import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useUsersStore, useItemsStore, useProjectTagsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	project_comments: (store, idProjet, data) => {
		store.comments[idProjet] = {};
		for (const comment of data) {
			store.comments[idProjet][comment.id_project_comment] = comment;
		}
	},
	project_documents: (store, idProjet, data) => {
		store.documents[idProjet] = {};
		for (const document of data) {
			store.documents[idProjet][document.id_project_document] = document;
		}
	},
	project_items: (store, idProjet, data) => {
		store.items[idProjet] = {};
		for (const item of data) {
			store.items[idProjet][item.id_item] = item;
		}
	},
	project_tags: (store, idProjet, data) => {
		store.projetTagProjet[idProjet] = {};
		for (const projetTagProjet of data) {
			store.projetTagProjet[idProjet][projetTagProjet.id_project_tag] = projetTagProjet;
		}
	},
	project_status_history: (store, idProjet, data) => {
		store.statusHistory[idProjet] = {};
		for (const statusHistory of data) {
			store.statusHistory[idProjet][statusHistory.id_project_status] = statusHistory;
		}
	},
};

function hydrateProjet(store, idProjet, project, expand = []) {
	store.commentairesTotalCount[idProjet] = project.project_comments_count;
	store.documentsTotalCount[idProjet] = project.project_documents_count;
	store.itemsTotalCount[idProjet] = project.project_items_count;
	store.projetTagProjetTotalCount[idProjet] = project.project_tags_count;
	store.statusHistoryTotalCount[idProjet] = project.project_status_history_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idProjet, project[key]);
		}
	}
}

const projetResource = createMainResource({
	path: () => "/project",
	idField: "id_project",
	stateKey: "projects",
	countKey: "projetsTotalCount",
	loadingKey: "projetsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateProjet(store, entity.id_project, entity, expand);
	},
});

const commentaireResource = createNestedResource({
	path: (idProjet) => `/project/${idProjet}/comment`,
	idField: "id_project_comment",
	stateKey: "comments",
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
	path: (idProjet) => `/project/${idProjet}/document`,
	idField: "id_project_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
});
const itemResource = createNestedResource({
	path: (idProjet) => `/project/${idProjet}/item`,
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
	path: (idProjet) => `/project/${idProjet}/project-tag`,
	idField: "id_project_tag",
	stateKey: "projetTagProjet",
	countKey: "projetTagProjetTotalCount",
	loadingKey: "projetTagProjetLoading",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("project_tag")) {
			const projetTagsStore = useProjectTagsStore();
			projetTagsStore.projectTags[entity.id_project_tag] = entity.project_tag;
		}
	},
});
const statusHistoryResource = createNestedResource({
	path: (idProjet) => `/project/${idProjet}/status-history`,
	idField: "id_project_status",
	stateKey: "statusHistory",
	countKey: "statusHistoryTotalCount",
	loadingKey: "statusHistoryLoading",
});

export const useProjectsStore = defineStore("projects",{
	state: () => ({
		projetsLoading: false,
		projetsTotalCount: 0,
		projects: {},
		projetEdition: {},

		commentairesLoading: false,
		commentairesTotalCount: {},
		comments: {},
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
			if (id !== "new" && this.projects[id]) {
				this.projetEdition[id] = {
					loading: false,
					name_project: this.projects[id].name_project,
					description_project: this.projects[id].description_project,
					url_project: this.projects[id].url_project,
					status_project: this.projects[id].status_project,
					date_start_project: this.projects[id].date_start_project,
					date_end_project: this.projects[id].date_end_project,
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
				url: `${baseUrl}/project/${idProjet}/document/${id}/download`,
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
