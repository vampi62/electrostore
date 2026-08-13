import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource, createNestedResource } from "@/helpers";

import { useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const EXPAND_HANDLERS = {
	projets_projet_tags: (store, idProjetTag, projetTag) => {
		store.projetTagsProjet[idProjetTag] = {};
		for (const projetTagProjet of projetTag.projets_projet_tags) {
			store.projetTagsProjet[idProjetTag][projetTagProjet.id_projet] = projetTagProjet;
		}
	},
};

function hydrateProjetTag(store, idProjetTag, projetTag, expand = []) {
	store.projetTagsProjetTotalCount[idProjetTag] = projetTag.projets_projet_tags_count;
	for (const key of expand) {
		if (EXPAND_HANDLERS[key]) {
			EXPAND_HANDLERS[key](store, idProjetTag, projetTag);
		}
	}
}

const projetTagResource = createMainResource({
	path: () => "/projet-tag",
	idField: "id_projet_tag",
	stateKey: "projetTags",
	countKey: "projetTagsTotalCount",
	loadingKey: "projetTagsLoading",
	onHydrate: (store, entity, expand) => {
		hydrateProjetTag(store, entity.id_projet_tag, entity, expand);
	},
});

const projetTagProjetResource = createNestedResource({
	path: (idProjetTag) => `/projet-tag/${idProjetTag}/projet`,
	idField: "id_projet",
	stateKey: "projetTagsProjet",
	countKey: "projetTagsProjetTotalCount",
	loadingKey: "projetTagsProjetLoading",
	onHydrate: (store, idProjetTag, entity, expand) => {
		if (expand.includes("projet")) {
			const projetsStore = useProjetsStore();
			projetsStore.projets[entity.id_projet] = entity.projet;
		}
	},
});

export const useProjetTagsStore = defineStore("projetTags",{
	state: () => ({
		projetTagsLoading: false,
		projetTagsTotalCount: 0,
		projetTags: {},
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
			if (id !== "new" && this.projetTags[id]) {
				this.projetTagEdition[id] = {
					loading: false,
					nom_projet_tag: this.projetTags[id].nom_projet_tag,
					poids_projet_tag: this.projetTags[id].poids_projet_tag,
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
