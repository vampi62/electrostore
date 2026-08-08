import { defineStore } from "pinia";

import { fetchWrapper, buildQuery } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

function hydrateCamera(store, idCamera, camera, expand = []) {
	store.cameras[idCamera] = camera;
	store.getStatus(idCamera);
}

export const useCamerasStore = defineStore("cameras",{
	state: () => ({
		loading: false,
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
			const paramString = buildQuery({ idResearch });
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				hydrateCamera(this, camera.id_camera, camera);
			}
			this.loading = false;
		},
		async getCameraByInterval(limit = 100, offset = 0, expand = [], filter = "", sort = "", clear = false) {
			this.loading = true;
			if (clear) {
				this.cameras = {};
			}
			const paramString = buildQuery({ limit, offset, expand, filter, sort });
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				hydrateCamera(this, camera.id_camera, camera);
			}
			this.TotalCount = newCameraList["pagination"]?.["total"] || 0;
			this.loading = false;
			return [newCameraList["pagination"]?.["nextOffset"] || 0, newCameraList["pagination"]?.["hasMore"] || false];
		},
		async getCameraById(id) {
			if (!this.cameras[id]) {
				this.cameras[id] = {};
			}
			this.cameras[id].loading = true;
			const camera = await fetchWrapper.get({
				url: `${baseUrl}/camera/${id}`,
				useToken: "access",
			});
			hydrateCamera(this, camera.id_camera, camera);
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
			return camera.id_camera;
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
