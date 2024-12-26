import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCamerasStore = defineStore({
    id: 'cameras',
    state: () => ({
        loading: true,
        TotalCount: 0,
        cameras: {},
        cameraEdition: {},
        stream: {},
        capture: {}
    }),
    actions: {
        async getCameraByList(idResearch = []) {
            this.loading = true;
            const idResearchString = idResearch.join(',');
            let newCameraList = await fetchWrapper.get({
                url: `${baseUrl}/camera?&idResearch=${idResearchString}`,
                useToken: "access"
            });
            for (const camera of newCameraList['data']) {
                this.cameras[camera.id_camera] = camera;
                this.getStatus(camera.id_camera);
                this.getCapture(camera.id_camera);
            }
            this.TotalCount = newCameraList['count'];
            this.loading = false;
        },
        async getCameraByInterval(limit = 100, offset = 0) {
            this.loading = true;
            let newCameraList = await fetchWrapper.get({
                url: `${baseUrl}/camera?limit=${limit}&offset=${offset}`,
                useToken: "access"
            });
            for (const camera of newCameraList['data']) {
                this.cameras[camera.id_camera] = camera;
                this.getStatus(camera.id_camera);
                this.getCapture(camera.id_camera);
            }
            this.TotalCount = newCameraList['count'];
            this.loading = false;
        },
        async getCameraById(id) {
            this.cameras[id] = { loading: true };
            this.cameras[id] = await fetchWrapper.get({
                url: `${baseUrl}/camera/${id}`,
                useToken: "access"
            });
            this.getStatus(id);
            this.getCapture(id);
        },
        async toggleLight(id) {
            this.camera = { loading: true };
            if (this.cameras[id].status?.ringLightPower > 0) {
                this.camera = await fetchWrapper.post({
                    url: `${baseUrl}/camera/${id}/light`,
                    useToken: "access",
                    body: { "state": false }
                });
            } else {
                this.camera = await fetchWrapper.post({
                    url: `${baseUrl}/camera/${id}/light`,
                    useToken: "access",
                    body: { "state": true }
                });
            }
            // waiting 0.5s
            this.getStatus(this.id_camera);
        },
        async getStream(id) {
            this.cameras[id].stream = await fetchWrapper.stream({
                url: `${baseUrl}/camera/${id}/stream`,
                useToken: "access"
            });
        },
        stopStream(id) {
            this.cameras[id].stream = null;
        },
        async getStatus(id) {
            this.cameras[id].status = { loading: true };
            this.cameras[id].status = await fetchWrapper.get({
                url: `${baseUrl}/camera/${id}/status`,
                useToken: "access"
            });
        },
        async getCapture(id) {
            const response = await fetchWrapper.image({
                url: `${baseUrl}/camera/${id}/capture`,
                useToken: "access"
            });
            const url = URL.createObjectURL(response);
            this.cameras[id].capture = url;
        },
        async createCamera(params) {
            this.cameraEdition = { loading: true };
            this.cameraEdition = await fetchWrapper.post({
                url: `${baseUrl}/camera`,
                useToken: "access",
                body: params
            });
            this.cameras[this.cameraEdition.id_camera] = this.cameraEdition;
        },
        async updateCamera(id, params) {
            this.cameraEdition = { loading: true };
            this.cameraEdition = await fetchWrapper.put({
                url: `${baseUrl}/camera/${id}`,
                useToken: "access",
                body: params
            });
            this.cameras[id] = this.cameraEdition;
        },
        async deleteCamera(id) {
            this.cameraEdition = { loading: true };
            this.cameraEdition = await fetchWrapper.delete({
                url: `${baseUrl}/camera/${id}`,
                useToken: "access"
            });
            delete this.cameras[id];
        }
    }
});
