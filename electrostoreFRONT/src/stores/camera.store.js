import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCamerasStore = defineStore("cameras",{
	state: () => ({
		loading: true,
		TotalCount: 0,
		cameras: {},
		status: {},
		cameraEdition: {},
		stream: {},
		capture: {},
	}),
	actions: {
		async getCameraByList(idResearch = []) {
			this.loading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].filter((str) => str).join("&");
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				this.cameras[camera.id_camera] = camera;
				this.getStatus(camera.id_camera);
			}
			this.TotalCount = newCameraList["count"];
			this.loading = false;
		},
		async getCameraByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.loading = true;
			if (clear) {
				this.cameras = {};
			}
			const offsetString = "offset=" + offset;
			const limitString = "limit=" + limit;
			const expandString = expand.map((id) => "expand=" + id.toString()).join("&");
			const filterString = filter ? "filter=" + filter : "";
			const sortString = sort ? "sort=" + sort : "";
			const paramString = [offsetString, limitString, expandString, filterString, sortString].filter((str) => str).join("&");
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				this.cameras[camera.id_camera] = camera;
				this.getStatus(camera.id_camera);
			}
			this.TotalCount = newCameraList["count"];
			this.loading = false;
		},
		async getCameraById(id) {
			if (!this.cameras[id]) {
				this.cameras[id] = {};
			}
			this.cameras[id].loading = true;
			this.cameras[id] = await fetchWrapper.get({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
			});
			this.getStatus(id);
		},
		async toggleLight(id) {
			if (!this.status[id]) {
				this.status[id] = {};
			}
			this.status[id].loading = true;
			if (this.status[id]?.ringLightPower > 0) {
				await fetchWrapper.post({
					url: `${baseUrl}/camera/${id}/light`,
					useToken: "access",
					body: { "state": false },
				});
			} else {
				await fetchWrapper.post({
					url: `${baseUrl}/camera/${id}/light`,
					useToken: "access",
					body: { "state": true },
				});
			}
			delete this.status[id].loading;
		},
		async getStream(id) {
			this.stream[id] = await fetchWrapper.stream({
				url: `${baseUrl}/camera/${id}/stream`,
				useToken: "access",
			});
		},
		stopStream(id) {
			this.stream[id] = null;
		},
		async getStatus(id) {
			if (!this.status[id]) {
				this.status[id] = {};
			}
			this.status[id].loading = true;
			this.status[id] = await fetchWrapper.get({
				url: `${baseUrl}/camera/${id}/status`,
				useToken: "access",
			});
		},
		async getCapture(id, getBlob = false) {
			const response = await fetchWrapper.image({
				url: `${baseUrl}/camera/${id}/capture`,
				useToken: "access",
			});
			if (getBlob) {
				this.capture[id] = response;
			} else {
				const url = URL.createObjectURL(response);
				this.capture[id] = url;
			}
		},
		async createCamera(params) {
			const camera = await fetchWrapper.post({
				url: `${baseUrl}/camera`,
				useToken: "access",
				body: params,
			});
			this.cameras[camera.id_camera] = camera;
		},
		async updateCamera(id, params) {
			this.cameras[id] = await fetchWrapper.put({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
				body: params,
			});
		},
		async deleteCamera(id) {
			await fetchWrapper.delete({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
			});
			delete this.cameras[id];
		},
	},
});
