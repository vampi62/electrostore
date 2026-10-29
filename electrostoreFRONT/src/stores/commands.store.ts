import { defineStore } from "pinia";

import { createResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useCarriersStore, useCommandsCommentsStore, useCommandsDocumentsStore, useCommandsHistoryStore, useItemsCommandsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";
type ReadCommandDto = components["schemas"]["ReadCommandDto"];
type ReadExtendedCommandDto = components["schemas"]["ReadExtendedCommandDto"];
type CreateCommandDto = components["schemas"]["CreateCommandDto"];
type UpdateCommandDto = components["schemas"]["UpdateCommandDto"];

// What an expanded command brings along is handed to the store of each nested resource
const EXPAND_HANDLERS: Record<string, (idCommand: string, data: ReadExtendedCommandDto) => void> = {
	command_comments: (idCommand, data) => useCommandsCommentsStore().hydrateCommandComments({ id_command: idCommand }, data.command_comments ?? []),
	commands_documents: (idCommand, data) => useCommandsDocumentsStore().hydrateCommandDocuments({ id_command: idCommand }, data.commands_documents ?? []),
	commands_history: (idCommand, data) => useCommandsHistoryStore().hydrateCommandHistory({ id_command: idCommand }, data.commands_history ?? []),
	commands_items: (idCommand, data) => useItemsCommandsStore().hydrateItemCommands({ id_command: idCommand }, data.commands_items ?? []),
	carrier: (idCommand, data) => {
		if (data.carrier?.id_carrier) {
			useCarriersStore().carriers[data.carrier.id_carrier] = data.carrier;
		}
	},
};

function hydrateCommand(idCommand: string, command: ReadExtendedCommandDto, expand: string[] = []) {
	const scope = { id_command: idCommand };
	useCommandsCommentsStore().hydrateCommandComments(scope, undefined, command.command_comments_count);
	useCommandsDocumentsStore().hydrateCommandDocuments(scope, undefined, command.commands_documents_count);
	useItemsCommandsStore().hydrateItemCommands(scope, undefined, command.commands_items_count);
	for (const key of expand) {
		EXPAND_HANDLERS[key]?.(idCommand, command);
	}
}

const commandResource = createResource<{ readExtended: ReadExtendedCommandDto, readBasic: ReadCommandDto, create: CreateCommandDto, update: UpdateCommandDto }>({
	compositeKey: ["id_command"],
	generatedField: "id_command",
	path: [{ keys: [], url: () => "/command" }],
	entityPath: [{ keys: ["id_command"], url: ({ id_command }) => `/command/${id_command}` }],
	stateKey: "commands",
	countKey: "commandsTotalCount",
	loadingKey: "commandsLoading",
	editionKey: "commandEdition",
	onHydrate: (store: StoreGeneric, entity: ReadExtendedCommandDto, expand) => {
		if (!entity?.id_command) {
			return;
		}
		hydrateCommand(String(entity.id_command), entity, expand);
	},
});

// Discards what is staged for the nested resources of a command; a comment being edited is saved on its own, saving the element must not discard it
function clearNestedEdition(idCommand: string) {
	const scope = { id_command: idCommand };
	useCommandsDocumentsStore().clearCommandDocumentEdition(scope);
	useItemsCommandsStore().clearItemCommandEdition(scope);
}

export const useCommandsStore = defineStore("commands", {
	state: () => ({
		commandsLoading: false,
		commandsTotalCount: 0,
		commands: {} as Record<string, ReadCommandDto>,
		commandEdition: {} as Record<string, Edition<{ create: CreateCommandDto, update: UpdateCommandDto }>>,
	}),
	actions: {
		getCommandByInterval: commandResource.single.getByInterval,
		getCommandById: commandResource.single.getById,
		createCommand: commandResource.single.create,
		getAvailableNewCommandId: commandResource.single.getAvailableNewId,
		updateCommand: commandResource.single.update,
		deleteCommand: commandResource.single.remove,
		loadToEdition(id: string, preset: string | null = null) {
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
					loading: false,
				};
			} else {
				this.commandEdition[id] = {
					loading: false,
					is_tracking_requested: false,
					tracking_number_command: "",
				};
				commandResource.single.loadEditionPreset.call(this, id, preset);
			}
			clearNestedEdition(id);
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.commandEdition[id]) {
				this.commandEdition[id] = {};
			}
			this.commandEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.commandEdition[id];
			clearNestedEdition(id);
			useCommandsCommentsStore().clearCommandCommentEdition({ id_command: id });
		},
		async saveAllChanges(id: string) {
			let realId = id;
			const documentsStore = useCommandsDocumentsStore();
			const itemsStore = useItemsCommandsStore();
			if (isNewId(id)) {
				realId = await this.createCommand(this.commandEdition[id] as CreateCommandDto);
				// what was staged for the new command now belongs to the saved one
				documentsStore.copyCommandDocumentByKeyPart("id_command", id, realId);
				itemsStore.copyItemCommandByKeyPart("id_command", id, realId);
			} else {
				await this.updateCommand(id, this.commandEdition[id]);
			}
			const scope = { id_command: realId };
			await Promise.all([
				documentsStore.pushCommandDocumentChange(scope),
				itemsStore.pushItemCommandChange(scope),
			]);
			return realId;
		},
	},
});
