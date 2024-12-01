import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCamerasStore = defineStore({
    id: 'cameras',
    state: () => ({
        cameras: {},
        camera: {},
        stream: {},
        capture: {},
        status: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.cameras = { loading: true };
            this.cameras = await fetchWrapper.get({
                url: `${baseUrl}/camera?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
            for (const camera of this.cameras) {
                this.getStatus(camera.id_camera);
            }
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.camera = { loading: true };
            this.camera = await fetchWrapper.get({
                url: `${baseUrl}/camera/${id}`,
                token: sessionTokenStore.token
            });
        },
        async toggleLight(id, state) {
            const sessionTokenStore = useSessionTokenStore();
            this.camera = { loading: true };
            this.camera = await fetchWrapper.post({
                url: `${baseUrl}/camera/${id}/light`,
                token: sessionTokenStore.token,
                body: {"state": state}
            });
        },
        getStream(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.stream[id] = fetchWrapper.stream({
                url: `${baseUrl}/camera/${id}/stream`,
                token: sessionTokenStore.token
            });
        },
        stopStream(id) {
            this.stream[id] = null;
        },
        async getStatus(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.status[id] = { loading: true };
            this.status[id] = await fetchWrapper.get({
                url: `${baseUrl}/camera/${id}/status`,
                token: sessionTokenStore.token
            });
        },
        async getCapture(id) {
            const sessionTokenStore = useSessionTokenStore();
            const response = await fetchWrapper.image({
                url: `${baseUrl}/camera/${id}/capture`,
                token: sessionTokenStore.token
            });
            const url = URL.createObjectURL(response);
            this.capture[id] = url;
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.camera = { loading: true };
            this.camera = await fetchWrapper.post({
                url: `${baseUrl}/camera`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.camera = { loading: true };
            this.camera = await fetchWrapper.put({
                url: `${baseUrl}/camera/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.camera = { loading: true };
            this.camera = await fetchWrapper.delete({
                url: `${baseUrl}/camera/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
