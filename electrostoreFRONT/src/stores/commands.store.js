import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";

import { useUsersStore, useCarriersStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	command_comments: (store, idCommand, data) => {
		store.comments[idCommand] = {};
		for (const comment of data) {
			store.comments[idCommand][comment.id_command_comment] = comment;
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
	store.commentsTotalCount[idCommand] = command.command_comments_count;
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

const commentResource = createNestedResource({
	path: (idCommand) => `/command/${idCommand}/comment`,
	idField: "id_command_comment",
	stateKey: "comments",
	countKey: "commentsTotalCount",
	loadingKey: "commentsLoading",
	editionKey: "commentEdition",
	readyKey: "commentReady",
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
	editionKey: "documentEdition",
	readyKey: "documentReady",
});
const itemResource = createNestedResource({
	path: (idCommand) => `/command/${idCommand}/item`,
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	editionKey: "itemEdition",
	readyKey: "itemReady",
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

		commentsTotalCount: {},
		commentsLoading: false,
		comments: {},
		commentEdition: {},
		commentReady: {},

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
					price_command: this.commands[id].price_command,
					url_command: this.commands[id].url_command,
					status_command: this.commands[id].status_command,
					date_command: this.commands[id].date_command,
					date_delivery_command: this.commands[id].date_delivery_command,
					tracking_number_command: this.commands[id].tracking_number_command,
					id_carrier: this.commands[id].id_carrier,
					is_tracking_requested: this.commands[id].is_tracking_requested,
					is_tracking_validated: this.commands[id].is_tracking_validated,
					is_active: this.commands[id].is_active,
					shipper_address_command: this.commands[id].shipper_address_command,
					recipient_address_command: this.commands[id].recipient_address_command,
					last_status_command: this.commands[id].last_status_command,
					loading: false,
				};
			} else {
				this.commandEdition[id] = {
					loading: false,
					is_tracking_requested: false,
					is_tracking_validated: false,
					is_active: true,
					tracking_number_command: "",
				};
			}
			this.commentEdition[id] = {};
			this.commentReady[id] = {};
			this.documentEdition[id] = {};
			this.documentReady[id] = {};
			this.itemEdition[id] = {};
			this.itemReady[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.commandEdition[id]) {
				this.commandEdition[id] = {};
			}
			this.commandEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.commandEdition[id];
			delete this.commentEdition[id];
			delete this.commentReady[id];
			delete this.documentEdition[id];
			delete this.documentReady[id];
			delete this.itemEdition[id];
			delete this.itemReady[id];
		},
		async saveAllChanges(id) {
			let realId = id;
			if (id === "new") {
				realId = await this.createCommand(this.commandEdition[id]);
				this.copyCommentAllId(id, realId);
				this.copyDocumentAllId(id, realId);
				this.copyItemAllId(id, realId);
			} else {
				await this.updateCommand(id, this.commandEdition[id]);
			}
			await Promise.all([
				this.pushCommentChange(realId),
				this.pushDocumentChange(realId),
				this.pushItemChange(realId),
			]);
			return realId;
		},

		getCommentByInterval: commentResource.getByInterval,
		getCommentById: commentResource.getById,
		createComment: commentResource.create,
		updateComment: commentResource.update,
		deleteComment: commentResource.remove,
		getAvailableNewCommentId: commentResource.getAvailableNewId,
		valideCommentEditionById: commentResource.valideEditionById,
		copyCommentPerId: commentResource.copyPerId,
		copyCommentAllId: commentResource.copyAllId,
		pushCommentChange: commentResource.pushChange,

		getDocumentByInterval: documentResource.getByInterval,
		getDocumentById: documentResource.getById,
		createDocument: documentResource.create,
		updateDocument: documentResource.update,
		deleteDocument: documentResource.remove,
		getAvailableNewDocumentId: documentResource.getAvailableNewId,
		valideDocumentEditionById: documentResource.valideEditionById,
		copyDocumentPerId: documentResource.copyPerId,
		copyDocumentAllId: documentResource.copyAllId,
		pushDocumentChange: documentResource.pushChange,
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
		getAvailableNewItemId: itemResource.getAvailableNewId,
		valideItemEditionById: itemResource.valideEditionById,
		copyItemPerId: itemResource.copyPerId,
		copyItemAllId: itemResource.copyAllId,
		pushItemChange: itemResource.pushChange,

		getHistoryByInterval: historyResource.getByInterval,
		getHistoryById: historyResource.getById,
	},
});
