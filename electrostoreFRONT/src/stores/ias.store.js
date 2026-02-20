import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useIasStore = defineStore("ias", {
	state: () => ({
		loading: true,
		TotalCount: 0,
		ias: {},
		iaEdition: {},
		status: { train: {}, start: {}, detect: {} },
	}),
	actions: {
		async getIaByList(idResearch = []) {
			this.loading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].filter((str) => str).join("&");
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
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
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
