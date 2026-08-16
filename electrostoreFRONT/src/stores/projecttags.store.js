import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useProjectsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	project_tags: (store, idProjectTag, projectTag) => {
		store.projectTagsProject[idProjectTag] = {};
		for (const projectTagProject of projectTag.project_tags) {
			store.projectTagsProject[idProjectTag][projectTagProject.id_project] = projectTagProject;
		}
	},
};

function hydrateProjectTag(store, idProjectTag, projectTag, expand = []) {
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
	onHydrate: (store, idProjectTag, entity, expand) => {
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
		projectTags: {},
		projectTagEdition: {},

		projectTagsProjectLoading: false,
		projectTagsProjectTotalCount: {},
		projectTagsProject: {},
		projectTagProjectEdition: {},
	}),
	actions: {
		getProjectTagByList: projectTagResource.getByList,
		getProjectTagByInterval: projectTagResource.getByInterval,
		getProjectTagById: projectTagResource.getById,
		createProjectTag: projectTagResource.create,
		updateProjectTag: projectTagResource.update,
		deleteProjectTag: projectTagResource.remove,
		createProjectTagBulk: projectTagResource.createBulk,
		loadToEdition(id, preset = null) {
			this.projectTagEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.projectTagEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.projectTags[id]) {
				this.projectTagEdition[id] = {
					loading: false,
					name_project_tag: this.projectTags[id].name_project_tag,
					weight_project_tag: this.projectTags[id].weight_project_tag,
				};
			} else {
				this.projectTagEdition[id] = {
					loading: false,
				};
			}
			this.projectTagProjectEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.projectTagEdition[id]) {
				this.projectTagEdition[id] = {};
			}
			this.projectTagEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.projectTagEdition[id];
			delete this.projectTagProjectEdition[id];
		},

		getProjectTagProjectByInterval: projectTagProjectResource.getByInterval,
		getProjectTagProjectById: projectTagProjectResource.getById,
		createProjectTagProject: projectTagProjectResource.create,
		deleteProjectTagProject: projectTagProjectResource.remove,
		createProjectTagProjectBulk: projectTagProjectResource.createBulk,
		deleteProjectTagProjectBulk: projectTagProjectResource.removeBulk,
	},
});
