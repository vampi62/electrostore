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
		projetTagReady: {},

		projetTagsProjetLoading: true,
		projetTagsProjetTotalCount: {},
		projetTagsProjet: {},
		projetTagProjetEdition: {},
		projetTagProjetReady: {},
	}),
	actions: {
		async getProjetTagByList(idResearch = [], expand = []) {
			this.projetTagsLoading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const paramString = [idResearchString, expandString].join("&");
			const newProjetTagList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag?${paramString}`,
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
			this.projetTagsLoading = false;
		},
		async getProjetTagByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.projetTagsLoading = true;
			if (clear) {
				this.projetTags = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newProjetTagList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag?${paramString}`,
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
			this.projetTagsTotalCount = newProjetTagList["pagination"]?.["total"] || 0;
			this.projetTagsLoading = false;
			return [newProjetTagList["pagination"]?.["nextOffset"] || 0, newProjetTagList["pagination"]?.["hasMore"] || false];
		},
		async getProjetTagById(id, expand = []) {
			if (!this.projetTags[id]) {
				this.projetTags[id] = {};
			}
			this.projetTags[id].loading = true;
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
			const projetTag = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag`,
				useToken: "access",
				body: params,
			});
			this.projetTags[projetTag.id_projet_tag] = projetTag;
			return projetTag.id_projet_tag;
		},
		async updateProjetTag(id, params) {
			if (params.nom_projet_tag === this.projetTags[id].nom_projet_tag) {
				delete params.nom_projet_tag;
			}
			this.projetTags[id] = await fetchWrapper.put({
				url: `${baseUrl}/projet-tag/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteProjetTag(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${id}`,
				useToken: "access",
			});
			delete this.projetTags[id];
		},
		async createProjetTagBulk(params) {
			const projetTagBulk = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTag of projetTagBulk["valide"]) {
				this.projetTags[projetTag.id_projet_tag] = projetTag;
			}
		},
		async pushProjetTagReady() {
			for (const id in this.projetTagReady) {
				const change = this.projetTagReady[id];
				if (change.status === "new") {
					const newId = await this.createProjetTag(change.data);
					this.updateIdProjetTag(id, newId);
					this.pushProjetTagProjetReady(newId);
					delete this.projetTagReady[id];
				} else if (change.status === "modified") {
					await this.updateProjetTag(id, change.data);
					this.pushProjetTagProjetReady(id);
					delete this.projetTagReady[id];
				} else if (change.status === "delete") {
					await this.deleteProjetTag(id);
					delete this.projetTagReady[id];
				}
			}
		},
		async pushProjetTagById(id) {
			if (!this.projetTagEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createProjetTag(this.projetTagEdition[id]);
				this.updateIdProjetTag(id, newId);
			} else {
				await this.updateProjetTag(id, this.projetTagEdition[id]);
			}
			this.pushProjetTagProjetReady(newId);
			return newId;
		},
		updateIdProjetTag(oldId, newId) {
			if (this.projetTagsProjetReady[oldId]) {
				this.projetTagsProjetReady[newId] = { ...this.projetTagsProjetReady[oldId], id_projetTag: newId };
				delete this.projetTagsProjetReady[oldId];
			}
			if (this.projetTagsProjetEdition[oldId]) {
				this.projetTagsProjetEdition[newId] = { ...this.projetTagsProjetEdition[oldId], id_projetTag: newId };
				delete this.projetTagsProjetEdition[oldId];
			}
		},
		commitProjetTagEdition(id, operation = "modified") { // return (sucess:bool, newStatus:string)
			if (!this.projetTagEdition[id]) {
				return { success: false, newStatus: null };
			}
			if (!this.projetTagReady[id]) {
				this.projetTagReady[id] = {};
			}
			if (this.projetTagReady[id].status === "new" && operation === "delete") {
				delete this.projetTagReady[id];
				return { success: true, newStatus: "delete" };
			} else if (this.projetTagReady[id].status === "modified" && operation === "delete") {
				this.projetTagReady[id].status = "delete";
			} else if (this.projetTagReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.projetTagReady[id].status = "modified";
			} else {
				this.projetTagReady[id].status = operation;
			}
			this.projetTagReady[id].data = { ...this.projetTagEdition[id] };
			return { success: true, newStatus: this.projetTagReady[id].status };
		},
		getAvailableEditionProjetTag() {
			// search existing "new{id}" in projetTagEdition to find available id for new COMMAND
			const newIds = Math.max(Object.keys(this.projetTagEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		clearProjetTagEdition() {
			this.projetTagEdition = {};
			this.projetTagReady = {};
		},
		clearProjetTagEditionById(id) {
			delete this.projetTagEdition[id];
			delete this.projetTagReady[id];
			this.clearProjetTagProjetEdition(id);
		},

		async getProjetTagProjetByInterval(idProjetTag, limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			if (!this.projetTagsProjet[idProjetTag] || clear) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			this.projetTagsProjetLoading = true;
			const projetsStore = useProjetsStore();
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newProjetTagProjetList = await fetchWrapper.get({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet?${paramString}`,
				useToken: "access",
			});
			for (const projetTagProjet of newProjetTagProjetList["data"]) {
				this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet] = projetTagProjet;
				if (expand.includes("projet")) {
					projetsStore.projets[projetTagProjet.id_projet] = projetTagProjet.projet;
				}
			}
			this.projetTagsProjetTotalCount[idProjetTag] = newProjetTagProjetList["pagination"]?.["total"] || 0;
			this.projetTagsProjetLoading = false;
			return [newProjetTagProjetList["pagination"]?.["nextOffset"] || 0, newProjetTagProjetList["pagination"]?.["hasMore"] || false];
		},
		async getProjetTagProjetById(idProjetTag, idProjet, expand = []) {
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			if (!this.projetTagsProjet[idProjetTag][idProjet]) {
				this.projetTagsProjet[idProjetTag][idProjet] = {};
			}
			this.projetTagsProjet[idProjetTag][idProjet].loading = true;
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
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			const projetTagProjet = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet`,
				useToken: "access",
				body: params,
			});
			this.projetTagsProjet[idProjetTag][params.id_projet] = projetTagProjet;
		},
		async deleteProjetTagProjet(idProjetTag, idProjet) {
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/${idProjet}`,
				useToken: "access",
			});
			delete this.projetTagsProjet[idProjetTag][idProjet];
		},
		async createProjetTagProjetBulk(idProjetTag, params) {
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			const projetTagProjetBulk = await fetchWrapper.post({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTagProjet of projetTagProjetBulk["valide"]) {
				this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet] = projetTagProjet;
			}
		},
		async deleteProjetTagProjetBulk(idProjetTag, params) {
			if (!this.projetTagsProjet[idProjetTag]) {
				this.projetTagsProjet[idProjetTag] = {};
			}
			const projetTagProjetBulk = await fetchWrapper.delete({
				url: `${baseUrl}/projet-tag/${idProjetTag}/projet/bulk`,
				useToken: "access",
				body: params,
			});
			for (const projetTagProjet of projetTagProjetBulk["valide"]) {
				delete this.projetTagsProjet[idProjetTag][projetTagProjet.id_projet];
			}
		},
		async pushProjetTagProjetReady(idProjetTag) {
			if (!this.projetTagsProjetReady[idProjetTag]) {
				return;
			}
			for (const id in this.projetTagsProjetReady[idProjetTag]) {
				const change = this.projetTagsProjetReady[idProjetTag][id];
				if (change.status === "new") {
					await this.createProjetTagProjet(idProjetTag, change.data);
					delete this.projetTagsProjetReady[id];
				} else if (change.status === "modified") {
					await this.updateProjetTagProjet(idProjetTag, id, change.data);
					delete this.projetTagsProjetReady[id];
				} else if (change.status === "delete") {
					await this.deleteProjetTagProjet(idProjetTag, id);
					delete this.projetTagsProjetReady[id];
				}
			}
		},
		commitProjetTagProjetEdition(idProjetTag, id, operation = "modified") {
			if (!this.projetTagsProjetEdition[idProjetTag] || !this.projetTagsProjetEdition[idProjetTag][id]) {
				return { success: false, newStatus: null };
			}
			if (!this.projetTagsProjetReady[idProjetTag]) {
				this.projetTagsProjetReady[idProjetTag] = {};
			}
			if (!this.projetTagsProjetReady[idProjetTag][id]) {
				this.projetTagsProjetReady[idProjetTag][id] = {};
			}
			if (this.projetTagsProjetReady[idProjetTag][id].status === "new" && operation === "delete") {
				delete this.projetTagsProjetReady[idProjetTag][id];
				return { success: true, newStatus: "delete" };
			} else if (this.projetTagsProjetReady[idProjetTag][id].status === "modified" && operation === "delete") {
				this.projetTagsProjetReady[idProjetTag][id].status = "delete";
			} else if (this.projetTagsProjetReady[idProjetTag][id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.projetTagsProjetReady[idProjetTag][id].status = "modified";
			} else {
				this.projetTagsProjetReady[idProjetTag][id].status = operation;
			}
			this.projetTagsProjetReady[idProjetTag][id].data = { ...this.projetTagsProjetEdition[idProjetTag][id] };
			return { success: true, newStatus: this.projetTagsProjetReady[id].status };
		},
		clearProjetTagProjetEdition(idProjetTag) {
			this.projetTagsProjetEdition[idProjetTag] = {};
			this.projetTagsProjetReady[idProjetTag] = {};
		},
	},
});
