import { defineStore } from "pinia";

import { fetchWrapper, createMainResource } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

function hydrateCamera(store, idCamera, camera, expand = []) {
	store.getStatus(idCamera);
}

const cameraResource = createMainResource({
	path: () => "/camera",
	idField: "id_camera",
	stateKey: "cameras",
	countKey: "TotalCount",
	loadingKey: "loading",
	onHydrate: (store, entity, expand) => {
		hydrateCamera(store, entity.id_camera, entity, expand);
	},
	/* onRemove: (store, id) => {
		delete store.status[id];
		delete store.stream[id];
		delete store.capture[id];
	}, */
});

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
		getCameraByList: cameraResource.getByList,
		getCameraByInterval: cameraResource.getByInterval,
		getCameraById: cameraResource.getById,
		createCamera: cameraResource.create,
		updateCamera: cameraResource.update,
		deleteCamera: cameraResource.remove,
		loadToEdition(id, preset = null) {
			this.cameraEdition[id] = {};
			if (preset) {
				preset.split(";").forEach((pair) => {
					const [key, value] = pair.split(":");
					if (key && value) {
						this.cameraEdition[id][key] = value;
					}
				});
			}
			if (id !== "new" && this.cameras[id]) {
				this.cameraEdition[id] = {
					loading: false,
					name_camera: this.cameras[id].name_camera,
					url_camera: this.cameras[id].url_camera,
					user_camera: this.cameras[id].user_camera,
					password_camera: this.cameras[id].password_camera,
				};
				this.cameraEdition[id]._check = (this.cameras[id].user_camera !== "") || (this.cameras[id].password_camera !== "");
			} else {
				this.cameraEdition[id] = {
					loading: false,
				};
			}
		},
		setLoadingEdition(id, loading) {
			if (!this.cameraEdition[id]) {
				this.cameraEdition[id] = {};
			}
			this.cameraEdition[id].loading = loading;
		},
		clearEdition(id) {
			delete this.cameraEdition[id];
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
			if (this.stream[id]) {
				delete this.stream[id];
			}
		},
		async getStatus(id) {
			if (!this.status[id]) {
				this.status[id] = {};
			}
			this.status[id].loading = true;
			try {
				this.status[id] = await fetchWrapper.get({
					url: `${baseUrl}/camera/${id}/status`,
					useToken: "access",
				});
			} catch (error) {
				console.error("Error fetching camera status:", error);
			}
			this.status[id].loading = false;
		},
		async getCapture(id, getBlob = false) {
			try {
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
			} catch (error) {
				console.error("Error fetching camera capture:", error);
			}
		},
	},
});
