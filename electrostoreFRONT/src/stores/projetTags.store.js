import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { useProjetsStore } from "@/stores";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useProjetTagsStore = defineStore("projetTags",{
	state: () => ({
		projetTagsLoading: true,
		projetTagsTotalCount: 0,
		projetTags: {},
		projetTagEdition: {},

		projetTagsProjetLoading: true,
		projetTagsProjetTotalCount: {},
		projetTagsProjet: {},
		projetTagProjetEdition: {},
	}),
	actions: {
		async getProjetTagByList(idResearch = [], expand = []) {
			this.projetTagsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newProjetTagList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag?${idResearchString}&${expandString}`,
				useToken: "access",
			});
			for (const projetTag of newProjetTagList["data"]) {
				this.projetTags[projetTag.id_projet_tag] = projetTag;
				this.projetTagsProjetTotalCount[projetTag.id_projet_tag] = projetTag.projets_projet_tags_count;
				if (expand.includes("projets_projet_tags")) {
					this.projetTagsProjet[projetTag.id_projet_tag] = {};
					for (const projetTagProjet of projetTag.projets_projet_tags) {
						this.projetTagsProjet[projetTag.id_projet_tag][projetTagProjet.id_projet] = projetTagProjet;
					}
				}
			}
			this.projetTagsTotalCount = newProjetTagList["count"];
			this.projetTagsLoading = false;
		},
		async getProjetTagByInterval(limit = 100, offset = 0, expand = []) {
			this.projetTagsLoading = true;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			let newProjetTagList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			for (const projetTag of newProjetTagList["data"]) {
				this.projetTags[projetTag.id_projet_tag] = projetTag;
				this.projetTagsProjetTotalCount[projetTag.id_projet_tag] = projetTag.projets_projet_tags_count;
				if (expand.includes("projets_projet_tags")) {
					this.projetTagsProjet[projetTag.id_projet_tag] = {};
					for (const projetTagProjet of projetTag.projets_projet_tags) {
						this.projetTagsProjet[projetTag.id_projet_tag][projetTagProjet.id_projet] = projetTagProjet;
					}
				}
			}
			this.projetTagsTotalCount = newProjetTagList["count"];
			this.projetTagsLoading = false;
		},
		async getProjetTagById(id, expand = []) {
			if (!this.projetTags[id]) {
				this.projetTags[id] = {};
			}
			this.projetTags[id] = { ...this.projetTags[id], loading: true };
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projetTags[id] = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag/${id}?${expandString}`,
				useToken: "access",
			});
			this.projetTagsProjetTotalCount[id] = this.projetTags[id].projets_projet_tags_count;
			if (expand.includes("projets_projet_tags")) {
				this.projetTagsProjet[id] = {};
				for (const projetTagProjet of this.projetTags[id].projets_projet_tags) {
					this.projetTagsProjet[id][projetTagProjet.id_projet] = projetTagProjet;
				}
			}
		},
		async createProjetTag(params) {
			this.projetTagEdition.loading = true;
			this.projetTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag`,
				useToken: "access",
				body: params,
			});
			this.projetTags[this.projetTagEdition.id_projet_tag] = this.projetTagEdition;
		},
		async updateProjetTag(id, params) {
			this.projetTagEdition.loading = true;
			if (params.nom_projet_tag === this.projetTags[id].nom_projet_tag) {
				delete params.nom_projet_tag;
			}
			this.projetTagEdition = await fetchWrapper.put({
				url: `${baseUrl}/projet-tag/${id}`,
				useToken: "access",
				body: params,
			});
			this.projetTags[id] = this.projetTagEdition;
		},
		async deleteProjetTag(id) {
			this.projetTagEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${id}`,
				useToken: "access",
			});
			delete this.projetTags[id];
			this.projetTagEdition = {};
		},
		async createProjetTagBulk(params) {
			this.projetTagEdition.loading = true;
			this.projetTagEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTag of this.projetTagEdition["valide"]) {
				this.projetTags[projetTag.id_projet_tag] = projetTag;
			}
		},

		async getProjetTagProjetByInterval(idProjetTag, limit = 100, offset = 0, expand = []) {
			this.projetTagsProjetLoading = true;
			const projetsStore = useProjetsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			let newProjetTagProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet?limit=${limit}&offset=${offset}&${expandString}`,
				useToken: "access",
			});
			this.projetTagsProjetTotalCount[idProjetTag] = newProjetTagProjetList["count"];
			for (const projetTagProjet of newProjetTagProjetList["data"]) {
				this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet] = projetTagProjet;
				if (expand.includes("projet")) {
					projetsStore.projets[projetTagProjet.id_projet] = projetTagProjet.projet;
				}
			}
			this.projetTagsProjetLoading = false;
		},
		async getProjetTagProjetById(idProjetTag, idProjet, expand = []) {
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			if (!this.projetTagsProjet[idProjetTag][idProjet]) {
				this.projetTagsProjet[idProjetTag][idProjet] = {};
			}
			this.projetTagsProjet[idProjetTag][idProjet] = { ...this.projetTagsProjet[idProjetTag][idProjet], loading: true };
			const projetsStore = useProjetsStore();
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			this.projetTagsProjet[idProjetTag][idProjet] = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/${idProjet}&${expandString}`,
				useToken: "access",
			});
			if (expand.includes("projet")) {
				projetsStore.projets[this.projetTagsProjet[idProjetTag].id_projet] = this.projetTagsProjet[idProjetTag].projet;
			}
		},
		async createProjetTagProjet(idProjetTag, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet`,
				useToken: "access",
				body: params,
			});
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			this.projetTagsProjet[idProjetTag][params.id_projet] = this.projetTagProjetEdition;
		},
		async deleteProjetTagProjet(idProjetTag, idProjet) {
			this.projetTagProjetEdition.loading = true;
			await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/${idProjet}`,
				useToken: "access",
			});
			delete this.projetTagsProjet[idProjetTag][idProjet];
			this.projetTagProjetEdition = {};
		},
		async createProjetTagProjetBulk(idProjetTag, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/bulk`,
				useToken: "access",
				body: params,
			});
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			for (const projetTagProjet of this.projetTagProjetEdition["valide"]) {
				this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet] = projetTagProjet;
			}
		},
		async deleteProjetTagProjetBulk(idProjetTag, params) {
			this.projetTagProjetEdition.loading = true;
			this.projetTagProjetEdition = await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTagProjet of this.projetTagProjetEdition["valide"]) {
				delete this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet];
			}
		},
	},
});
