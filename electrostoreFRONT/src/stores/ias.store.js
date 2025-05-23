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
			let newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?${idResearchString}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				this.ias[ia.id_ia] = ia;
			}
			this.TotalCount = newIaList["count"];
			this.loading = false;
		},
		async getIaByInterval(limit = 100, offset = 0) {
			this.loading = true;
			let newIaList = await fetchWrapper.get({
				url: `${baseUrl}/ia?limit=${limit}&offset=${offset}`,
				useToken: "access",
			});
			for (const ia of newIaList["data"]) {
				this.ias[ia.id_ia] = ia;
			}
			this.TotalCount = newIaList["count"];
			this.loading = false;
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
			this.iaEdition.loading = true;
			this.iaEdition = await fetchWrapper.post({
				url: `${baseUrl}/ia`,
				useToken: "access",
				body: params,
			});
			this.ias[this.iaEdition.id_ia] = this.iaEdition;
		},
		async updateIa(id, params) {
			this.iaEdition.loading = true;
			this.iaEdition = await fetchWrapper.put({
				url: `${baseUrl}/ia/${id}`,
				useToken: "access",
				body: params,
			});
			this.ias[id] = params;
		},
		async deleteIa(id) {
			this.iaEdition.loading = true;
			this.iaEdition = await fetchWrapper.delete({
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
			// if params is a Blob, convert it to a File
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
