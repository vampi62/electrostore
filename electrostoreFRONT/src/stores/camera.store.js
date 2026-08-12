import { defineStore } from "pinia";

import { fetchWrapper, buildQuery, createMainResource } from "@/helpers";

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
