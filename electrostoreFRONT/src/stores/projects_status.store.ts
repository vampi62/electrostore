import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import type { components } from "@/types/api";

type ReadProjectStatusDto = components["schemas"]["ReadProjectStatusDto"];

const projectStatusResource = createResource<{ readBasic: ReadProjectStatusDto }>({
	compositeKey: ["id_project", "id_project_status"],
	path: [
		{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}/status-history` },
	],
	entityPath: [
		{ keys: ["id_project", "id_project_status"], url: ({ id_project, id_project_status }) => `/project/${id_project}/status-history/${id_project_status}` },
	],
	stateKey: "projectStatus",
	countKey: "projectStatusTotalCount",
	loadingKey: "projectStatusLoading",
});

// Records are indexed by the composite key "<id_project>:<id_project_status>"
export const useProjectsStatusStore = defineStore("projectsStatus", {
	state: () => ({
		projectStatusLoading: false,
		// indexed by scope: "id_project=3"
		projectStatusTotalCount: {} as Record<string, number>,
		projectStatus: {} as Record<string, ReadProjectStatusDto>,
	}),
	getters: {
		// the records of a project, indexed by the rest of the key
		projectStatusByProject: (state) => (id: string) => projectStatusResource.slice(state.projectStatus, { id_project: id }),
		projectStatusCountByProject: (state) => (id: string) => state.projectStatusTotalCount[projectStatusResource.toScopeKey({ id_project: id })] ?? 0,
	},
	actions: {
		getProjectStatusByInterval: projectStatusResource.getByInterval,
		getProjectStatusById: projectStatusResource.getById,
		hydrateProjectStatus: projectStatusResource.hydrate,
	},
});
