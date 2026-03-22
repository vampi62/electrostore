import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCamerasStore = defineStore("cameras",{
	state: () => ({
		loading: true,
		TotalCount: 0,
		cameras: {},
		cameraEdition: {},
		cameraReady: {},
		
		status: {},
		stream: {},
		capture: {},
	}),
	actions: {
		async getCameraByList(idResearch = []) {
			this.loading = true;
			const idResearchString = idResearch.map((id) => "idResearch=" + id.toString()).join("&");
			const paramString = [idResearchString].join("&");
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				this.cameras[camera.id_camera] = camera;
				this.getStatus(camera.id_camera);
			}
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
			const paramString = [offsetString, limitString, expandString, filterString, sortString].join("&");
			const newCameraList = await fetchWrapper.get({
				url: `${baseUrl}/camera?${paramString}`,
				useToken: "access",
			});
			for (const camera of newCameraList["data"]) {
				this.cameras[camera.id_camera] = camera;
				this.getStatus(camera.id_camera);
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
		commitCameraEdition(id, operation = "modified") {
			if (!this.cameraEdition[id]) {
				return;
			}
			if (!this.cameraReady[id]) {
				this.cameraReady[id] = {};
			}
			if (this.cameraReady[id].status === "new" && operation === "delete") {
				delete this.cameraReady[id];
				return;
			} else if (this.cameraReady[id].status === "modified" && operation === "delete") {
				this.cameraReady[id].status = "delete";
			} else if (this.cameraReady[id].status === "delete" && (operation === "modified" || operation === "new")) {
				this.cameraReady[id].status = "modified";
			} else {
				this.cameraReady[id].status = operation;
			}
			this.cameraReady[id].data = { ...this.cameraEdition[id] };
		},
		async pushCameraReady() {
			for (const id in this.cameraReady) {
				const change = this.cameraReady[id];
				if (change.status === "new") {
					await this.createCamera(change.data);
					delete this.cameraReady[id];
				} else if (change.status === "modified") {
					await this.updateCamera(id, change.data);
					delete this.cameraReady[id];
				} else if (change.status === "delete") {
					await this.deleteCamera(id);
					delete this.cameraReady[id];
				}
			}
		},
		clearCameraEditeur() {
			this.cameraEdition = {};
			this.cameraReady = {};
		},
		async pushCameraById(id) {
			if (!this.cameraEdition[id]) {
				return;
			}
			let newId = id;
			if (id.startsWith("new")) {
				newId = await this.createCamera(this.cameraEdition[id]);
				this.updateIdCamera(id, newId);
			} else {
				await this.updateCamera(id, this.cameraEdition[id]);
			}
			return newId;
		},
		clearCameraEditionById(id) {
			delete this.cameraEdition[id];
			delete this.cameraReady[id];
		},
		getAvailableEditionCamera() {
			// search existing "new{id}" in cameraEdition to find available id for new CAMERA
			const newIds = Math.max(Object.keys(this.cameraEdition).filter((id) => id.startsWith("new")).map((id) => parseInt(id.replace("new", ""))), 0);
			return "new" + (newIds + 1);
		},
		updateIdCamera(oldId, newId) {
		},
	},
});
