import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";

import type { Edition, Ready, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";

type ReadCommandDocumentDto = components["schemas"]["ReadCommandDocumentDto"];
type CreateCommandDocumentByCommandDto = WithFile<NonNullable<paths["/api/command/{id_command}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"], "document">;
type UpdateCommandDocumentDto = components["schemas"]["UpdateCommandDocumentDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const commandDocumentResource = createResource<{ readBasic: ReadCommandDocumentDto, create: CreateCommandDocumentByCommandDto, update: UpdateCommandDocumentDto }>({
	compositeKey: ["id_command", "id_command_document"],
	generatedField: "id_command_document",
	path: [
		{ keys: ["id_command"], url: ({ id_command }) => `/command/${id_command}/document` },
	],
	entityPath: [
		{ keys: ["id_command", "id_command_document"], url: ({ id_command, id_command_document }) => `/command/${id_command}/document/${id_command_document}` },
	],
	stateKey: "commandDocuments",
	countKey: "commandDocumentsTotalCount",
	loadingKey: "commandDocumentsLoading",
	editionKey: "commandDocumentEdition",
	readyKey: "commandDocumentReady",
});

// Records are indexed by the composite key "<id_command>:<id_command_document>"
export const useCommandsDocumentsStore = defineStore("commandsDocuments", {
	state: () => ({
		commandDocumentsLoading: false,
		// indexed by scope: "id_command=3"
		commandDocumentsTotalCount: {} as Record<string, number>,
		commandDocuments: {} as Record<string, ReadCommandDocumentDto>,
		commandDocumentEdition: {} as Record<string, Edition<{ create: CreateCommandDocumentByCommandDto, update: UpdateCommandDocumentDto }>>,
		commandDocumentReady: {} as Record<string, Ready<{ create: CreateCommandDocumentByCommandDto, update: UpdateCommandDocumentDto }>>,
	}),
	getters: {
		// the records of a command, indexed by the rest of the key
		commandDocumentsByCommand: (state) => (id: string) => commandDocumentResource.slice(state.commandDocuments, { id_command: id }),
		commandDocumentsCountByCommand: (state) => (id: string) => state.commandDocumentsTotalCount[commandDocumentResource.toScopeKey({ id_command: id })] ?? 0,
		commandDocumentEditionByCommand: (state) => (id: string) => commandDocumentResource.live(state.commandDocumentEdition, { id_command: id }),
		commandDocumentReadyByCommand: (state) => (id: string) => commandDocumentResource.live(state.commandDocumentReady, { id_command: id }),
	},
	actions: {
		getCommandDocumentByInterval: commandDocumentResource.getByInterval,
		getCommandDocumentById: commandDocumentResource.getById,
		createCommandDocument: commandDocumentResource.create,
		updateCommandDocument: commandDocumentResource.update,
		deleteCommandDocument: commandDocumentResource.remove,
		hydrateCommandDocuments: commandDocumentResource.hydrate,
		getAvailableNewCommandDocumentId: commandDocumentResource.getAvailableNewId,
		setCommandDocumentEdition: commandDocumentResource.setEdition,
		removeCommandDocumentEdition: commandDocumentResource.removeEdition,
		removeCommandDocumentReady: commandDocumentResource.removeReady,
		setCommandDocumentStagedSlice: commandDocumentResource.setStagedSlice,
		valideCommandDocumentEditionByKey: commandDocumentResource.valideEditionByKey,
		copyCommandDocumentByKeyPart: commandDocumentResource.copyByKeyPart,
		clearCommandDocumentEdition: commandDocumentResource.clearEdition,
		pushCommandDocumentChange: commandDocumentResource.pushChange,
		async downloadCommandDocument(idCommand: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/command/${idCommand}/document/${id}/download`,
				useToken: "access",
			});
		},
	},
});
