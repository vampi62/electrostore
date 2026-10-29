import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useUsersStore, useProjectsStore } from "@/stores";

import type { Edition } from "@/types/resource";
import type { components } from "@/types/api";

type ReadProjectCommentDto = components["schemas"]["ReadProjectCommentDto"];
type ReadExtendedProjectCommentDto = components["schemas"]["ReadExtendedProjectCommentDto"];
type CreateProjectCommentByProjectDto = components["schemas"]["CreateProjectCommentByProjectDto"];
type UpdateProjectCommentDto = components["schemas"]["UpdateProjectCommentDto"];

const projectCommentResource = createResource<{ readExtended: ReadExtendedProjectCommentDto, readBasic: ReadProjectCommentDto, create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>({
	compositeKey: ["id_project", "id_project_comment"],
	generatedField: "id_project_comment",
	path: [
		{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}/comment` },
		{ keys: ["id_user"], url: ({ id_user }) => `/user/${id_user}/project_comment` },
	],
	entityPath: [
		{ keys: ["id_project", "id_project_comment"], url: ({ id_project, id_project_comment }) => `/project/${id_project}/comment/${id_project_comment}` },
	],
	stateKey: "projectComments",
	countKey: "projectCommentsTotalCount",
	loadingKey: "projectCommentsLoading",
	editionKey: "projectCommentEdition",
	onHydrate: (store, entity: ReadExtendedProjectCommentDto, expand) => {
		if (expand.includes("user") && entity.user && entity.id_user) {
			useUsersStore().users[entity.id_user] = entity.user;
		}
		if (expand.includes("project") && entity.project?.id_project) {
			useProjectsStore().projects[entity.project.id_project] = entity.project;
		}
	},
});

// Records are indexed by the composite key "<id_project>:<id_project_comment>"
export const useProjectsCommentsStore = defineStore("projectsComments", {
	state: () => ({
		projectCommentsLoading: false,
		// indexed by scope: "id_project=3"
		projectCommentsTotalCount: {} as Record<string, number>,
		projectComments: {} as Record<string, ReadProjectCommentDto>,
		projectCommentEdition: {} as Record<string, Edition<{ create: CreateProjectCommentByProjectDto, update: UpdateProjectCommentDto }>>,
	}),
	getters: {
		// the records of a project, indexed by the rest of the key
		projectCommentsByProject: (state) => (id: string) => projectCommentResource.slice(state.projectComments, { id_project: id }),
		projectCommentsCountByProject: (state) => (id: string) => state.projectCommentsTotalCount[projectCommentResource.toScopeKey({ id_project: id })] ?? 0,
		projectCommentEditionByProject: (state) => (id: string) => projectCommentResource.live(state.projectCommentEdition, { id_project: id }),
		// the records loaded for a user (a field of the record, not of the key), indexed by id_project_comment
		projectCommentsByUser: (state) => (id: string) => Object.fromEntries(Object.entries(state.projectComments).filter(([, record]) => String(record.id_user) === id).map(([, record]) => [String(record.id_project_comment), record])),
		projectCommentsCountByUser: (state) => (id: string) => state.projectCommentsTotalCount[projectCommentResource.toScopeKey({ id_user: id })] ?? 0,
	},
	actions: {
		getProjectCommentByInterval: projectCommentResource.getByInterval,
		getProjectCommentById: projectCommentResource.getById,
		createProjectComment: projectCommentResource.create,
		updateProjectComment: projectCommentResource.update,
		deleteProjectComment: projectCommentResource.remove,
		hydrateProjectComments: projectCommentResource.hydrate,
		getAvailableNewProjectCommentId: projectCommentResource.getAvailableNewId,
		setProjectCommentEdition: projectCommentResource.setEdition,
		removeProjectCommentEdition: projectCommentResource.removeEdition,
		clearProjectCommentEdition: projectCommentResource.clearEdition,
	},
});
