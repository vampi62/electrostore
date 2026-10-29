import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type ReadCommandHistoryDto = components["schemas"]["ReadCommandHistoryDto"];

const commandHistoryResource = createResource<{ readBasic: ReadCommandHistoryDto }>({
	compositeKey: ["id_command", "id_command_history"],
	path: [
		{ keys: ["id_command"], url: ({ id_command }) => `/command/${id_command}/history` },
	],
	entityPath: [
		{ keys: ["id_command", "id_command_history"], url: ({ id_command, id_command_history }) => `/command/${id_command}/history/${id_command_history}` },
	],
	stateKey: "commandHistory",
	countKey: "commandHistoryTotalCount",
	loadingKey: "commandHistoryLoading",
});

// Records are indexed by the composite key "<id_command>:<id_command_history>"
export const useCommandsHistoryStore = defineStore("commandsHistory", {
	state: () => ({
		commandHistoryLoading: false,
		// indexed by scope: "id_command=3"
		commandHistoryTotalCount: {} as Record<string, number>,
		commandHistory: {} as Record<string, ReadCommandHistoryDto>,
	}),
	getters: {
		// the records of a command, indexed by the rest of the key
		commandHistoryByCommand: (state) => (id: string) => commandHistoryResource.slice(state.commandHistory, { id_command: id }),
		commandHistoryCountByCommand: (state) => (id: string) => state.commandHistoryTotalCount[commandHistoryResource.toScopeKey({ id_command: id })] ?? 0,
	},
	actions: {
		getCommandHistoryByInterval: commandHistoryResource.getByInterval,
		getCommandHistoryById: commandHistoryResource.getById,
		hydrateCommandHistory: commandHistoryResource.hydrate,
	},
});
