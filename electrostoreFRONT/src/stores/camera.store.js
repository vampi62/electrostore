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
			let newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${idResearchString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				this.cameras[camera.id_camera] = camera;
				this.getStatus(camera.id_camera);
			}
			this.TotalCount = newCameraList["count"];
			this.loading = false;
		},
		async getCameraByInterval(limit = 100, offset = 0) {
			this.loading = true;
			let newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?limit=${limit}&offset=${offset}`,
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
			this.cameraEdition.loading = true;
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
			this.cameraEdition.loading = true;
			this.cameraEdition = await fetchWrapper.post({
				url: `${baseUrl}/camera`,
				useToken: "access",
				body: params,
			});
			this.cameras[this.cameraEdition.id_camera] = this.cameraEdition;
		},
		async updateCamera(id, params) {
			this.cameraEdition.loading = true;
			this.cameraEdition = await fetchWrapper.put({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
				body: params,
			});
			this.cameras[id] = this.cameraEdition;
		},
		async deleteCamera(id) {
			this.cameraEdition.loading = true;
			this.cameraEdition = await fetchWrapper.delete({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
			});
			delete this.cameras[id];
		},
	},
});
