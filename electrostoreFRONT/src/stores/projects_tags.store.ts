import { defineStore } from "pinia";

import { createResource } from "@/helpers";

import { useProjectsStore, useProjectTagsStore } from "@/stores";

import type { Edition, Ready } from "@/types/resource";
import type { components } from "@/types/api";

type ReadProjectProjectTagDto = components["schemas"]["ReadProjectProjectTagDto"];
type ReadExtendedProjectProjectTagDto = components["schemas"]["ReadExtendedProjectProjectTagDto"];
type CreateProjectProjectTagByProjectDto = components["schemas"]["CreateProjectProjectTagByProjectDto"];
type CreateProjectProjectTagByProjectTagDto = components["schemas"]["CreateProjectProjectTagByProjectTagDto"];
type CreateProjectProjectTag = CreateProjectProjectTagByProjectDto | CreateProjectProjectTagByProjectTagDto;

const projectProjectTagResource = createResource<{ readExtended: ReadExtendedProjectProjectTagDto, readBasic: ReadProjectProjectTagDto, create: CreateProjectProjectTag, createBulk: CreateProjectProjectTag[], deleteBulk: number[] }>({
	compositeKey: ["id_project", "id_project_tag"],
	path: [
		{ keys: ["id_project"], url: ({ id_project }) => `/project/${id_project}/project-tag` },
		{ keys: ["id_project_tag"], url: ({ id_project_tag }) => `/project-tag/${id_project_tag}/project` },
	],
	entityPath: [
		{ keys: ["id_project", "id_project_tag"], url: ({ id_project, id_project_tag }) => `/project/${id_project}/project-tag/${id_project_tag}` },
	],
	stateKey: "projectProjectTags",
	countKey: "projectProjectTagsTotalCount",
	loadingKey: "projectProjectTagsLoading",
	editionKey: "projectProjectTagEdition",
	readyKey: "projectProjectTagReady",
	onHydrate: (store, entity: ReadExtendedProjectProjectTagDto, expand) => {
		if (expand.includes("project_tag") && entity.project_tag && entity.id_project_tag) {
			useProjectTagsStore().projectTags[entity.id_project_tag] = entity.project_tag;
		}
		if (expand.includes("project") && entity.project && entity.id_project) {
			useProjectsStore().projects[entity.id_project] = entity.project;
		}
	},
});

// Records are indexed by the composite key "<id_project>:<id_project_tag>"
export const useProjectsTagsStore = defineStore("projectsTags", {
	state: () => ({
		projectProjectTagsLoading: false,
		// indexed by scope: "id_project=3"
		projectProjectTagsTotalCount: {} as Record<string, number>,
		projectProjectTags: {} as Record<string, ReadProjectProjectTagDto>,
		projectProjectTagEdition: {} as Record<string, Edition<{ create: CreateProjectProjectTag }>>,
		projectProjectTagReady: {} as Record<string, Ready<{ create: CreateProjectProjectTag }>>,
	}),
	getters: {
		// the records of a project, indexed by the rest of the key
		projectProjectTagsByProject: (state) => (id: string) => projectProjectTagResource.slice(state.projectProjectTags, { id_project: id }),
		projectProjectTagsCountByProject: (state) => (id: string) => state.projectProjectTagsTotalCount[projectProjectTagResource.toScopeKey({ id_project: id })] ?? 0,
		projectProjectTagsGroupedByProject: (state) => projectProjectTagResource.group(state.projectProjectTags, "id_project"),
		projectProjectTagEditionByProject: (state) => (id: string) => projectProjectTagResource.live(state.projectProjectTagEdition, { id_project: id }),
		projectProjectTagReadyByProject: (state) => (id: string) => projectProjectTagResource.live(state.projectProjectTagReady, { id_project: id }),
		// the records of a projecttag, indexed by the rest of the key
		projectProjectTagsByProjectTag: (state) => (id: string) => projectProjectTagResource.slice(state.projectProjectTags, { id_project_tag: id }),
		projectProjectTagsCountByProjectTag: (state) => (id: string) => state.projectProjectTagsTotalCount[projectProjectTagResource.toScopeKey({ id_project_tag: id })] ?? 0,
		projectProjectTagsGroupedByProjectTag: (state) => projectProjectTagResource.group(state.projectProjectTags, "id_project_tag"),
		projectProjectTagEditionByProjectTag: (state) => (id: string) => projectProjectTagResource.live(state.projectProjectTagEdition, { id_project_tag: id }),
		projectProjectTagReadyByProjectTag: (state) => (id: string) => projectProjectTagResource.live(state.projectProjectTagReady, { id_project_tag: id }),
	},
	actions: {
		getProjectProjectTagByInterval: projectProjectTagResource.getByInterval,
		getProjectProjectTagById: projectProjectTagResource.getById,
		createProjectProjectTag: projectProjectTagResource.create,
		deleteProjectProjectTag: projectProjectTagResource.remove,
		createProjectProjectTagBulk: projectProjectTagResource.createBulk,
		deleteProjectProjectTagBulk: projectProjectTagResource.removeBulk,
		hydrateProjectProjectTags: projectProjectTagResource.hydrate,
		setProjectProjectTagEdition: projectProjectTagResource.setEdition,
		removeProjectProjectTagEdition: projectProjectTagResource.removeEdition,
		removeProjectProjectTagReady: projectProjectTagResource.removeReady,
		setProjectProjectTagStagedSlice: projectProjectTagResource.setStagedSlice,
		valideProjectProjectTagEditionByKey: projectProjectTagResource.valideEditionByKey,
		copyProjectProjectTagByKeyPart: projectProjectTagResource.copyByKeyPart,
		clearProjectProjectTagEdition: projectProjectTagResource.clearEdition,
		pushProjectProjectTagChange: projectProjectTagResource.pushChange,
	},
});
