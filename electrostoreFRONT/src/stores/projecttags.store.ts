import { defineStore } from "pinia";

import { createMainResource, createNestedResource } from "@/helpers";
import { isNewId } from "@/utils";

import { useProjectsStore } from "@/stores";

import type { StoreGeneric } from "pinia";
import type { components } from "@/types/api";
type ReadProjectTagDto = components["schemas"]["ReadProjectTagDto"];
type ReadProjectProjectTagDto = components["schemas"]["ReadProjectProjectTagDto"];

const EXPAND_HANDLERS: Record<string, (store: StoreGeneric, idProjectTag: string, data: any) => void> = {
	project_tags: (store, idProjectTag, data) => {
		store.projectTagsProject[idProjectTag] = {};
		for (const projectTagProject of data.project_tags) {
			store.projectTagsProject[idProjectTag][projectTagProject.id_project] = projectTagProject;
		}
	},
};

function hydrateProjectTag(store: StoreGeneric, idProjectTag: string, projectTag: any, expand: string[] = []) {
	store.projectTagsProjectTotalCount[idProjectTag] = projectTag.project_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idProjectTag, projectTag);
		}
	}
}

const projectTagResource = createMainResource({
	path: () => "/project-tag",
	idField: "id_project_tag",
	stateKey: "projectTags",
	countKey: "projectTagsTotalCount",
	loadingKey: "projectTagsLoading",
	editionKey: "projectTagEdition",
	onHydrate: (store, entity, expand) => {
		hydrateProjectTag(store, entity.id_project_tag, entity, expand);
	},
});

const projectTagProjectResource = createNestedResource({
	path: (idProjectTag) => `/project-tag/${idProjectTag}/project`,
	idField: "id_project",
	stateKey: "projectTagsProject",
	countKey: "projectTagsProjectTotalCount",
	loadingKey: "projectTagsProjectLoading",
	editionKey: "projectTagProjectEdition",
	readyKey: "projectTagProjectReady",
	onHydrate: (store, entity, expand) => {
		if (expand.includes("project")) {
			const projectsStore = useProjectsStore();
			projectsStore.projects[entity.id_project] = entity.project;
		}
	},
});

export const useProjectTagsStore = defineStore("projectTags",{
	state: () => ({
		projectTagsLoading: false,
		projectTagsTotalCount: 0,
		projectTags: {} as Record<string, ReadProjectTagDto>,
		projectTagEdition: {} as Record<string, any>,

		projectTagsProjectLoading: false,
		projectTagsProjectTotalCount: {} as Record<string, number>,
		projectTagsProject: {} as Record<string, ReadProjectProjectTagDto>,
		projectTagProjectEdition: {} as Record<string, any>,
		projectTagProjectReady: {} as Record<string, any>,
	}),
	actions: {
		getProjectTagByList: projectTagResource.getByList,
		getProjectTagByInterval: projectTagResource.getByInterval,
		getProjectTagById: projectTagResource.getById,
		createProjectTag: projectTagResource.create,
		getAvailableNewProjectTagId: projectTagResource.getAvailableNewId,
		updateProjectTag: projectTagResource.update,
		deleteProjectTag: projectTagResource.remove,
		createProjectTagBulk: projectTagResource.createBulk,
		loadToEdition(id: string, preset = null) {
			if (!isNewId(id) && this.projectTags[id]) {
				this.projectTagEdition[id] = {
					loading: false,
					name_project_tag: this.projectTags[id].name_project_tag,
					weight_project_tag: this.projectTags[id].weight_project_tag,
				};
			} else {
				this.projectTagEdition[id] = {
					loading: false,
				};
				projectTagResource.loadEditionPreset.call(this, id, preset);
			}
			this.projectTagProjectEdition[id] = {};
			this.projectTagProjectReady[id] = {};
		},
		setLoadingEdition(id: string, loading: boolean) {
			if (!this.projectTagEdition[id]) {
				this.projectTagEdition[id] = {};
			}
			this.projectTagEdition[id].loading = loading;
		},
		clearEdition(id: string) {
			delete this.projectTagEdition[id];
			delete this.projectTagProjectEdition[id];
			delete this.projectTagProjectReady[id];
		},
		async saveAllChanges(id: string) {
			let realId = id;
			if (isNewId(id)) {
				realId = await this.createProjectTag(this.projectTagEdition[id]);
				this.copyProjectTagProjectAllId(id, realId);
			} else {
				await this.updateProjectTag(realId, this.projectTagEdition[id]);
			}
			await Promise.all([
				this.pushProjectTagProjectChange(realId),
			]);
			await this.getProjectTagById(realId, ["project_tags"]);
			return realId;
		},

		getProjectTagProjectByInterval: projectTagProjectResource.getByInterval,
		getProjectTagProjectById: projectTagProjectResource.getById,
		createProjectTagProject: projectTagProjectResource.create,
		deleteProjectTagProject: projectTagProjectResource.remove,
		createProjectTagProjectBulk: projectTagProjectResource.createBulk,
		deleteProjectTagProjectBulk: projectTagProjectResource.removeBulk,
		getAvailableNewProjectTagProjectId: projectTagProjectResource.getAvailableNewId,
		valideProjectTagProjectEditionById: projectTagProjectResource.valideEditionById,
		copyProjectTagProjectPerId: projectTagProjectResource.copyPerId,
		copyProjectTagProjectAllId: projectTagProjectResource.copyAllId,
		pushProjectTagProjectChange: projectTagProjectResource.pushChange,
	},
});
