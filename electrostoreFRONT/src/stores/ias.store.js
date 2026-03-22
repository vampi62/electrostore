import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useIasStore = defineStore("ias", {
	state: () => ({
		loading: true,
		TotalCount: 0,
		ias: {},
		iaEdition: {},
		iaReady: {},
		
		status: { train: {}, start: {}, detect: {} },
	}),
	actions: {
		async getIaByList(idResearch = []) {
			this.loading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].join("&");
			const newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?${paramString}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				this.ias[ia.id_ia] = ia;
			}
			this.loading = false;
		},
		async getIaByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.loading = true;
			if (clear) {
				this.ias = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?${paramString}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				this.ias[ia.id_ia] = ia;
			}
			this.TotalCount = newIaList["pagination"]?.["total"] || 0;
			this.loading = false;
			return [newIaList["pagination"]?.["nextOffset"] || 0, newIaList["pagination"]?.["hasMore"] || false];
		},
		async getIaById(id) {
			if (!this.ias[id]) {
				this.ias[id] = {};
			}
			this.ias[id].loading = true;
			this.ias[id] = await fetchWrapper.get({
				url: `${baseUrl}/ia/${id}`,
				useToken: "access",
			});
		},
		async createIa(params) {
			const ia = await fetchWrapper.post({
				url: `${baseUrl}/ia`,
				useToken: "access",
				body: params,
			});
			this.ias[ia.id_ia] = ia;
			return ia.id_ia;
		},
		async updateIa(id, params) {
			this.ias[id] = await fetchWrapper.put({
				url: `${baseUrl}/ia/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteIa(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/ia/${id}`,
				useToken: "access",
			});
			delete this.ias[id];
		},
		async getTrainStatus(id) {
			this.status.train.loading = true;
			this.status.train = await fetchWrapper.get({
				url: `${baseUrl}/ia/${id}/status`,
				useToken: "access",
			});
		},
		async startTrain(id) {
			this.status.start.loading = true;
			this.status.start = await fetchWrapper.post({
				url: `${baseUrl}/ia/${id}/train`,
				useToken: "access",
			});
		},
		async detectItem(id, params) {
			this.status.detect.loading = true;
			if (params instanceof Blob) {
				params = new File([params], "img_file.jpg", { type: params.type });
			}
			const formData = new FormData();
			formData.append("img_file", params);
			this.status.detect = await fetchWrapper.post({
				url: `${baseUrl}/ia/${id}/detect`,
				useToken: "access",
				body: formData,
				contentFile: true,
			});
		},
		commitIaEdition(id, operation = "modified") {
			if (!this.iaEdition[id]) {
				return;
			}
			if (!this.iaReady[id]) {
				this.iaReady[id] = {};
			}
			if (this.iaReady[id].status === "new" && operation === "delete") {
				delete this.iaReady[id];
				return;
			} else if (this.iaReady[id].status === "modified" && operation === "delete") {
				this.iaReady[id].status = "delete";
			} else if (this.iaReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.iaReady[id].status = "modified";
			} else {
				this.iaReady[id].status = operation;
			}
			this.iaReady[id].data = { ...this.iaEdition[id] };
		},
		async pushIaReady() {
			for (const id in this.iaReady) {
				const change = this.iaReady[id];
				if (change.status === "new") {
					await this.createIa(change.data);
					delete this.iaReady[id];
				} else if (change.status === "modified") {
					await this.updateIa(id, change.data);
					delete this.iaReady[id];
				} else if (change.status === "delete") {
					await this.deleteIa(id);
					delete this.iaReady[id];
				}
			}
		},
		clearIaEditeur() {
			this.iaEdition = {};
			this.iaReady = {};
		},
		async pushIaById(id) {
			if (!this.iaEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createIa(this.iaEdition[id]);
				this.updateIdIa(id, newId);
			} else {
				await this.updateIa(id, this.iaEdition[id]);
			}
			return newId;
		},
		clearIaEditionById(id) {
			delete this.iaEdition[id];
			delete this.iaReady[id];
		},
		getAvailableEditionIa() {
			// search existing "new{id}" in iaEdition to find available id for new IA
			const newIds = Math.max(Object.keys(this.iaEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		updateIdIa(oldId, newId) {
		},
	},
});
