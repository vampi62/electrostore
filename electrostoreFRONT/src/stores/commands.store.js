import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useUsersStore, useItemsStore, useCarriersStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	commands_commentaires: (store, idCommand, data) => {
		store.commentaires[idCommand] = {};
		for (const commentaire of data) {
			store.commentaires[idCommand][commentaire.id_command_commentaire] = commentaire;
		}
	},
	commands_documents: (store, idCommand, data) => {
		store.documents[idCommand] = {};
		for (const document of data) {
			store.documents[idCommand][document.id_command_document] = document;
		}
	},
	commands_history: (store, idCommand, data) => {
		store.history[idCommand] = {};
		for (const historyEntry of data) {
			store.history[idCommand][historyEntry.id_command_history] = historyEntry;
		}
	},
	commands_items: (store, idCommand, data) => {
		store.items[idCommand] = {};
		for (const item of data) {
			store.items[idCommand][item.id_item] = item;
		}
	},
	carrier: (store, idCommand, data) => {
		if (data) {
			const carriersStore = useCarriersStore();
			carriersStore.carriers[data.id_carrier] = data;
		}
	},
};

function hydrateCommand(store, idCommand, command, expand = []) {
	store.commentairesTotalCount[idCommand] = command.commands_commentaires_count;
	store.documentsTotalCount[idCommand] = command.commands_documents_count;
	store.itemsTotalCount[idCommand] = command.commands_items_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idCommand, command[key]);
		}
	}
}

const commandResource = createMainResource({
	path: () => "/command",
	idField: "id_command",
	stateKey: "commands",
	countKey: "commandsTotalCount",
	loadingKey: "commandsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateCommand(store, entity.id_command, entity, expand);
	},
});

const commentaireResource = createNestedResource({
	path: (idCommand) => `/command/${idCommand}/commentaire`,
	idField: "id_command_commentaire",
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
	path: (idCommand) => `/command/${idCommand}/document`,
	idField: "id_command_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
});
const itemResource = createNestedResource({
	path: (idCommand) => `/command/${idCommand}/item`,
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
});
const historyResource = createNestedResource({
	path: (idCommand) => `/command/${idCommand}/history`,
	idField: "id_command_history",
	stateKey: "history",
	countKey: "historyTotalCount",
	loadingKey: "historyLoading",
});

export const useCommandsStore = defineStore("commands",{
	state: () => ({
		commandsLoading: false,
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

		historyTotalCount: {},
		historyLoading: false,
		history: {},
	}),
	actions: {
		getCommandByList: commandResource.getByList,
		getCommandByInterval: commandResource.getByInterval,
		getCommandById: commandResource.getById,
		createCommand: commandResource.create,
		updateCommand: commandResource.update,
		deleteCommand: commandResource.remove,

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
		async downloadDocument(idCommand, id) {
			return await fetchWrapper.image({
				url: `${baseUrl}/command/${idCommand}/document/${id}/download`,
				useToken: "access",
			});
		},

		getItemByInterval: itemResource.getByInterval,
		getItemById: itemResource.getById,
		createItem: itemResource.create,
		updateItem: itemResource.update,
		deleteItem: itemResource.remove,
		createItemBulk: itemResource.createBulk,

		getHistoryByInterval: historyResource.getByInterval,
		getHistoryById: historyResource.getById,
	},
});
