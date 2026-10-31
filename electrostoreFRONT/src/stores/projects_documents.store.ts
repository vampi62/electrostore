import { defineStore } from "pinia";

import { fetchWrapper, createResource } from "@/helpers";

import type { Edition, Ready, WithFile } from "@/types/resource";
import type { components, paths } from "@/types/api";

type ReadProjectDocumentDto = components["schemas"]["ReadProjectDocumentDto"];
type CreateProjectDocumentByProjectDto = WithFile<NonNullable<paths["/api/project/{id_project}/document"]["post"]["requestBody"]>["content"]["multipart/form-data"], "document">;
type UpdateProjectDocumentDto = components["schemas"]["UpdateProjectDocumentDto"];

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const projectDocumentResource = createResource<{ readBasic: ReadProjectDocumentDto, create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>({
	compositeKey: ["id_project", "id_project_document"],
	generatedField: "id_project_document",
	path: [
		{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}/document` },
	],
	entityPath: [
		{ keys: ["id_project", "id_project_document"], url: ({ id_project, id_project_document }) => `/project/${id_project}/document/${id_project_document}` },
	],
	stateKey: "projectDocuments",
	countKey: "projectDocumentsTotalCount",
	loadingKey: "projectDocumentsLoading",
	editionKey: "projectDocumentEdition",
	readyKey: "projectDocumentReady",
});

// Records are indexed by the composite key "<id_project>:<id_project_document>"
export const useProjectsDocumentsStore = defineStore("projectsDocuments", {
	state: () => ({
		projectDocumentsLoading: false,
		// indexed by scope: "id_project=3"
		projectDocumentsTotalCount: {} as Record<string, number>,
		projectDocuments: {} as Record<string, ReadProjectDocumentDto>,
		projectDocumentEdition: {} as Record<string, Edition<{ create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>>,
		projectDocumentReady: {} as Record<string, Ready<{ create: CreateProjectDocumentByProjectDto, update: UpdateProjectDocumentDto }>>,
	}),
	getters: {
		// the records of a project, indexed by the rest of the key
		projectDocumentsByProject: (state) => (id: string) => projectDocumentResource.slice(state.projectDocuments, { id_project: id }),
		projectDocumentsCountByProject: (state) => (id: string) => state.projectDocumentsTotalCount[projectDocumentResource.toScopeKey({ id_project: id })] ?? 0,
		projectDocumentEditionByProject: (state) => (id: string) => projectDocumentResource.live(state.projectDocumentEdition, { id_project: id }),
		projectDocumentReadyByProject: (state) => (id: string) => projectDocumentResource.live(state.projectDocumentReady, { id_project: id }),
	},
	actions: {
		getProjectDocumentByInterval: projectDocumentResource.getByInterval,
		getProjectDocumentById: projectDocumentResource.getById,
		createProjectDocument: projectDocumentResource.create,
		updateProjectDocument: projectDocumentResource.update,
		deleteProjectDocument: projectDocumentResource.remove,
		hydrateProjectDocuments: projectDocumentResource.hydrate,
		getAvailableNewProjectDocumentId: projectDocumentResource.getAvailableNewId,
		setProjectDocumentEdition: projectDocumentResource.setEdition,
		removeProjectDocumentEdition: projectDocumentResource.removeEdition,
		removeProjectDocumentReady: projectDocumentResource.removeReady,
		setProjectDocumentStagedSlice: projectDocumentResource.setStagedSlice,
		valideProjectDocumentEditionByKey: projectDocumentResource.valideEditionByKey,
		copyProjectDocumentByKeyPart: projectDocumentResource.copyByKeyPart,
		clearProjectDocumentEdition: projectDocumentResource.clearEdition,
		pushProjectDocumentChange: projectDocumentResource.pushChange,
		async downloadProjectDocument(idProject: string, id: string) {
			return await fetchWrapper.image({
				url: `${baseUrl}/project/${idProject}/document/${id}/download`,
				useToken: "access",
			});
		},
	},
});
