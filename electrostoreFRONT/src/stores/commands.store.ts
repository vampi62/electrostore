import { defineStore } from "pinia";

import { fetchWrapper, createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useUsersStore, useCarriersStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { components, paths } from "@/types/api";
type ReadCommandDto = components["schemas"]["ReadCommandDto"];
type ReadExtendedCommandDto = components["schemas"]["ReadExtendedCommandDto"];
type CreateCommandDto = components["schemas"]["CreateCommandDto"];
type UpdateCommandDto = components["schemas"]["UpdateCommandDto"];

type ReadCommandCommentDto = components["schemas"]["ReadCommandCommentDto"];
type ReadExtendedCommandCommentDto = components["schemas"]["ReadExtendedCommandCommentDto"];
type CreateCommandCommentByCommandDto = components["schemas"]["CreateCommandCommentByCommandDto"];
type UpdateCommandCommentDto = components["schemas"]["UpdateCommandCommentDto"];

type ReadCommandDocumentDto = components["schemas"]["ReadCommandDocumentDto"];
type CreateCommandDocumentByCommandDto = NonNullable<paths["/api/command/{id_command}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"];
type UpdateCommandDocumentDto = components["schemas"]["UpdateCommandDocumentDto"];

type ReadCommandItemDto = components["schemas"]["ReadCommandItemDto"];
type ReadExtendedCommandItemDto = components["schemas"]["ReadExtendedCommandItemDto"];
type CreateCommandItemByCommandDto = components["schemas"]["CreateCommandItemByCommandDto"];
type UpdateCommandItemDto = components["schemas"]["UpdateCommandItemDto"];

type ReadCommandHistoryDto = components["schemas"]["ReadCommandHistoryDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idCommand: string, data: any[]) => void> = {
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

function hydrateCommand(store: StoreGeneric, idCommand: string, command: ReadExtendedCommandDto, expand: string[] = []) {
	store.commentsTotalCount[idCommand] = command.command_comments_count;
	store.documentsTotalCount[idCommand] = command.commands_documents_count;
	store.itemsTotalCount[idCommand] = command.commands_items_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idCommand, command[key]);
		}
	}
}

type EditionCommandDto = {
	loading?: boolean;
} & Partial<ReadCommandDto>;

type EditionCommandCommentDto = {
	loading?: boolean;
} & Partial<ReadCommandCommentDto>;

type EditionCommandItemDto = {
	loading?: boolean;
} & Partial<ReadCommandItemDto>;

type EditionCommandDocumentDto = {
	loading?: boolean;
} & Partial<ReadCommandDocumentDto>;

const commandResource = createMainResource<{ readExtended: ReadExtendedCommandDto, readBasic: ReadCommandDto, create: CreateCommandDto, update: UpdateCommandDto }>({
	path: () => "/command",
	idField: "id_command",
	stateKey: "commands",
	countKey: "commandsTotalCount",
	loadingKey: "commandsLoading",
	editionKey: "commandEdition",
	onHydrate: (store, entity: ReadExtendedCommandDto, expand) => {
		if (!entity || !entity.id_command) {
			return;
		}
		hydrateCommand(store, String(entity.id_command), entity, expand);
	},
});

const commentResource = createNestedResource<{ readExtended: ReadExtendedCommandCommentDto, readBasic: ReadCommandCommentDto, create: CreateCommandCommentByCommandDto, update: UpdateCommandCommentDto }>({
	path: (idCommand) => `/command/${idCommand}/comment`,
	idField: "id_command_comment",
	stateKey: "comments",
	countKey: "commentsTotalCount",
	loadingKey: "commentsLoading",
	editionKey: "commentEdition",
	onHydrate: (store, entity: ReadExtendedCommandCommentDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			const usersStore = useUsersStore();
			usersStore.users[entity.id_user] = entity.user;
		}
	},
});
const documentResource = createNestedResource<{ readExtended: ReadCommandDocumentDto, create: CreateCommandDocumentByCommandDto, update: UpdateCommandDocumentDto }>({
	path: (idCommand) => `/command/${idCommand}/document`,
	idField: "id_command_document",
	stateKey: "documents",
	countKey: "documentsTotalCount",
	loadingKey: "documentsLoading",
	editionKey: "documentEdition",
	readyKey: "documentReady",
});
const itemResource = createNestedResource<{ readExtended: ReadExtendedCommandItemDto, readBasic: ReadCommandItemDto, create: CreateCommandItemByCommandDto, update: UpdateCommandItemDto }>({
	path: (idCommand) => `/command/${idCommand}/item`,
	idField: "id_item",
	stateKey: "items",
	countKey: "itemsTotalCount",
	loadingKey: "itemsLoading",
	editionKey: "itemEdition",
	readyKey: "itemReady",
});
const historyResource = createNestedResource<{ readExtended: ReadCommandHistoryDto }>({
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
		commands: {} as Record<string, ReadCommandDto>,
		commandEdition: {} as Record<string, EditionCommandDto>,

		commentsTotalCount: {} as Record<string, number>,
		commentsLoading: false,
		comments: {} as Record<string, Record<string, ReadCommandCommentDto>>,
		commentEdition: {} as Record<string, EditionCommandCommentDto>,

		documentsTotalCount: {} as Record<string, number>,
		documentsLoading: false,
		documents: {} as Record<string, Record<string, ReadCommandDocumentDto>>,
		documentEdition: {} as Record<string, EditionCommandDocumentDto>,
		documentReady: {} as Record<string, any>,

		itemsTotalCount: {} as Record<string, number>,
		itemsLoading: false,
		items: {} as Record<string, Record<string, ReadCommandItemDto>>,
		itemEdition: {} as Record<string, EditionCommandItemDto>,
		itemReady: {} as Record<string, any>,

		historyTotalCount: {} as Record<string, number>,
		historyLoading: false,
		history: {} as Record<string, Record<string, ReadCommandHistoryDto>>,
	}),
	actions: {
		getCommandByList: commandResource.getByList,
		getCommandByInterval: commandResource.getByInterval,
		getCommandById: commandResource.getById,
		createCommand: commandResource.create,
		getAvailableNewCommandId: commandResource.getAvailableNewId,
		updateCommand: commandResource.update,
		deleteCommand: commandResource.remove,
		loadToEdition(id: string, preset = null) {
			if (!isNewId(id) && this.commands[id]) {
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
				commandResource.loadEditionPreset.call(this, id, preset);
			}
			this.commentEdition[id] = {};
			this.documentEdition[id] = {};
			this.documentReady[id] = {};
			this.itemEdition[id] = {};
			this.itemReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.commandEdition[id]) {
				this.commandEdition[id] = {};
			}
			this.commandEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.commandEdition[id];
			delete this.commentEdition[id];
			delete this.documentEdition[id];
			delete this.documentReady[id];
			delete this.itemEdition[id];
			delete this.itemReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createCommand(this.commandEdition[id] as CreateCommandDto);
				this.copyDocumentAllId(id, realId);
				this.copyItemAllId(id, realId);
			} else {
				await this.updateCommand(id, this.commandEdition[id]);
			}
			await Promise.all([
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
		async downloadDocument(idCommand: string, id: string) {
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
