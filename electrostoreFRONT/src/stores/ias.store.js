import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const iaResource = createMainResource({
	path: () => "/ia",
	idField: "id_ia",
	stateKey: "ias",
	countKey: "TotalCount",
	loadingKey: "loading",
});

export const useIasStore = defineStore("ias", {
	state: () => ({
		loading: false,
		TotalCount: 0,
		ias: {},
		iaEdition: {},
		status: { train: {}, start: {}, detect: {} },
	}),
	actions: {
		getIaByList: iaResource.getByList,
		getIaByInterval: iaResource.getByInterval,
		getIaById: iaResource.getById,
		createIa: iaResource.create,
		updateIa: iaResource.update,
		deleteIa: iaResource.remove,
		loadToEdition(id, preset = null) {
			this.iaEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.iaEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.ias[id]) {
				this.iaEdition[id] = {
					loading: false,
					name_ia: this.ias[id].name_ia,
					description_ia: this.ias[id].description_ia,
					date_ia: this.ias[id].date_ia,
					trained_ia: this.ias[id].trained_ia,
				};
			} else {
				this.iaEdition[id] = {
					loading: false,
				};
			}
		},
		setLoadingEdition(id, loading) {
			if (!this.iaEdition[id]) {
				this.iaEdition[id] = {};
			}
			this.iaEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.iaEdition[id];
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
