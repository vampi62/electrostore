import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useUsersStore, useCommandsStore } from "@/stores";

import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadCommandCommentDto = components["schemas"]["ReadCommandCommentDto"];
type ReadExtendedCommandCommentDto = components["schemas"]["ReadExtendedCommandCommentDto"];
type CreateCommandCommentByCommandDto = components["schemas"]["CreateCommandCommentByCommandDto"];
type UpdateCommandCommentDto = components["schemas"]["UpdateCommandCommentDto"];

const commandCommentResource = createResource<{ readExtended: ReadExtendedCommandCommentDto, readBasic: ReadCommandCommentDto, create: CreateCommandCommentByCommandDto, update: UpdateCommandCommentDto }>({
	compositeKey: ["id_command", "id_command_comment"],
	generatedField: "id_command_comment",
	path: [
		{ keys: ["id_command"], url: ({ id_command }) => `/command/${id_command}/comment` },
		{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}/command_comment` },
	],
	entityPath: [
		{ keys: ["id_command", "id_command_comment"], url: ({ id_command, id_command_comment }) => `/command/${id_command}/comment/${id_command_comment}` },
	],
	stateKey: "commandComments",
	countKey: "commandCommentsTotalCount",
	loadingKey: "commandCommentsLoading",
	editionKey: "commandCommentEdition",
	onHydrate: (store, entity: ReadExtendedCommandCommentDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			useUsersStore().users[entity.id_user] = entity.user;
		}
		if (expand.includes("command") && entity.command?.id_command) {
			useCommandsStore().commands[entity.command.id_command] = entity.command;
		}
	},
});

// Records are indexed by the composite key "<id_command>:<id_command_comment>"
export const useCommandsCommentsStore = defineStore("commandsComments", {
	state: () => ({
		commandCommentsLoading: false,
		// indexed by scope: "id_command=3"
		commandCommentsTotalCount: {} as Record<string, number>,
		commandComments: {} as Record<string, ReadCommandCommentDto>,
		commandCommentEdition: {} as Record<string, Edition<{ create: CreateCommandCommentByCommandDto, update: UpdateCommandCommentDto }>>,
	}),
	getters: {
		// the records of a command, indexed by the rest of the key
		commandCommentsByCommand: (state) => (id: string) => commandCommentResource.slice(state.commandComments, { id_command: id }),
		commandCommentsCountByCommand: (state) => (id: string) => state.commandCommentsTotalCount[commandCommentResource.toScopeKey({ id_command: id })] ?? 0,
		commandCommentEditionByCommand: (state) => (id: string) => commandCommentResource.live(state.commandCommentEdition, { id_command: id }),
		// the records loaded for a user (a field of the record, not of the key), indexed by id_command_comment
		commandCommentsByUser: (state) => (id: string) => Object.fromEntries(Object.entries(state.commandComments).filter(([, record]) => String(record.id_user) === id).map(([, record]) => [String(record.id_command_comment), record])),
		commandCommentsCountByUser: (state) => (id: string) => state.commandCommentsTotalCount[commandCommentResource.toScopeKey({ id_user: id })] ?? 0,
	},
	actions: {
		getCommandCommentByInterval: commandCommentResource.getByInterval,
		getCommandCommentById: commandCommentResource.getById,
		createCommandComment: commandCommentResource.create,
		updateCommandComment: commandCommentResource.update,
		deleteCommandComment: commandCommentResource.remove,
		hydrateCommandComments: commandCommentResource.hydrate,
		getAvailableNewCommandCommentId: commandCommentResource.getAvailableNewId,
		setCommandCommentEdition: commandCommentResource.setEdition,
		removeCommandCommentEdition: commandCommentResource.removeEdition,
		clearCommandCommentEdition: commandCommentResource.clearEdition,
	},
});
