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
		loadToEdition(id, preset = null) {
			this.commandEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.commandEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.commands[id]) {
				this.commandEdition[id] = {
					prix_command: this.commands[id].prix_command,
					url_command: this.commands[id].url_command,
					status_command: this.commands[id].status_command,
					date_command: this.commands[id].date_command,
					date_livraison_command: this.commands[id].date_livraison_command,
					tracking_number: this.commands[id].tracking_number,
					id_carrier: this.commands[id].id_carrier,
					is_tracking_requested: this.commands[id].is_tracking_requested,
					is_tracking_validated: this.commands[id].is_tracking_validated,
					is_active: this.commands[id].is_active,
					shipper_adress: this.commands[id].shipper_adress,
					recipient_adress: this.commands[id].recipient_adress,
					last_status: this.commands[id].last_status,
					loading: false,
				};
			} else {
				this.commandEdition[id] = {
					loading: false,
					is_tracking_requested: false,
					is_tracking_validated: false,
					is_active: true,
					tracking_number: "",
				};
			}
			this.commentaireEdition[id] = {};
			this.documentEdition[id] = {};
			this.itemEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.commandEdition[id]) {
				this.commandEdition[id] = {};
			}
			this.commandEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.commandEdition[id];
			delete this.commentaireEdition[id];
			delete this.documentEdition[id];
			delete this.itemEdition[id];
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
