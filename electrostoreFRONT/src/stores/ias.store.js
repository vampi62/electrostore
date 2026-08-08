import { defineStore } from "pinia";

import { fetchWrapper, buildQuery } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

function hydrateIa(store, idIa, ia) {
	store.ias[idIa] = ia;
}

export const useIasStore = defineStore("ias", {
	state: () => ({
		loading: false,
		TotalCount: 0,
		ias: {},
		iaEdition: {},
		status: { train: {}, start: {}, detect: {} },
	}),
	actions: {
		async getIaByList(idResearch = []) {
			this.loading = true;
			const paramString = buildQuery({ idResearch });
			const newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?${paramString}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				hydrateIa(this, ia.id_ia, ia);
			}
			this.loading = false;
		},
		async getIaByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.loading = true;
			if (clear) {
				this.ias = {};
			}
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?${paramString}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				hydrateIa(this, ia.id_ia, ia);
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
			const ia = await fetchWrapper.get({
				url: `${baseUrl}/ia/${id}`,
				useToken: "access",
			});
			hydrateIa(this, ia.id_ia, ia);
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
	},
});
