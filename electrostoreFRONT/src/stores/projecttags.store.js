import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useProjectsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	project_tags: (store, idProjetTag, projectTag) => {
		store.projetTagsProjet[idProjetTag] = {};
		for (const projetTagProjet of projectTag.project_tags) {
			store.projetTagsProjet[idProjetTag][projetTagProjet.id_project] = projetTagProjet;
		}
	},
};

function hydrateProjetTag(store, idProjetTag, projectTag, expand = []) {
	store.projetTagsProjetTotalCount[idProjetTag] = projectTag.project_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idProjetTag, projectTag);
		}
	}
}

const projetTagResource = createMainResource({
	path: () => "/project-tag",
	idField: "id_project_tag",
	stateKey: "projectTags",
	countKey: "projetTagsTotalCount",
	loadingKey: "projetTagsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateProjetTag(store, entity.id_project_tag, entity, expand);
	},
});

const projetTagProjetResource = createNestedResource({
	path: (idProjetTag) => `/project-tag/${idProjetTag}/project`,
	idField: "id_project",
	stateKey: "projetTagsProjet",
	countKey: "projetTagsProjetTotalCount",
	loadingKey: "projetTagsProjetLoading",
	onHydrate: (store, idProjetTag, entity, expand) => {
		if (expand.includes("project")) {
			const projetsStore = useProjectsStore();
			projetsStore.projects[entity.id_project] = entity.project;
		}
	},
});

export const useProjectTagsStore = defineStore("projectTags",{
	state: () => ({
		projetTagsLoading: false,
		projetTagsTotalCount: 0,
		projectTags: {},
		projetTagEdition: {},

		projetTagsProjetLoading: false,
		projetTagsProjetTotalCount: {},
		projetTagsProjet: {},
		projetTagProjetEdition: {},
	}),
	actions: {
		getProjetTagByList: projetTagResource.getByList,
		getProjetTagByInterval: projetTagResource.getByInterval,
		getProjetTagById: projetTagResource.getById,
		createProjetTag: projetTagResource.create,
		updateProjetTag: projetTagResource.update,
		deleteProjetTag: projetTagResource.remove,
		createProjetTagBulk: projetTagResource.createBulk,
		loadToEdition(id, preset = null) {
			this.projetTagEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.projetTagEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.projectTags[id]) {
				this.projetTagEdition[id] = {
					loading: false,
					name_project_tag: this.projectTags[id].name_project_tag,
					weight_project_tag: this.projectTags[id].weight_project_tag,
				};
			} else {
				this.projetTagEdition[id] = {
					loading: false,
				};
			}
			this.projetTagProjetEdition[id] = {};
		},
		setLoadingEdition(id, loading) {
			if (!this.projetTagEdition[id]) {
				this.projetTagEdition[id] = {};
			}
			this.projetTagEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.projetTagEdition[id];
			delete this.projetTagProjetEdition[id];
		},

		getProjetTagProjetByInterval: projetTagProjetResource.getByInterval,
		getProjetTagProjetById: projetTagProjetResource.getById,
		createProjetTagProjet: projetTagProjetResource.create,
		deleteProjetTagProjet: projetTagProjetResource.remove,
		createProjetTagProjetBulk: projetTagProjetResource.createBulk,
		deleteProjetTagProjetBulk: projetTagProjetResource.removeBulk,
	},
});
