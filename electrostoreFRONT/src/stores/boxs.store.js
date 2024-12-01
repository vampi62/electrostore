import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useBoxsStore = defineStore({
    id: 'boxs',
    state: () => ({
        boxs: {},
        box: {}
    }),
    actions: {
        async getAllBox(idStore, limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.boxs = { loading: true };
            this.boxs = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/box?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getBoxById(idStore, id) {
            const sessionTokenStore = useSessionTokenStore();
            this.box = { loading: true };
            this.box = await fetchWrapper.get({
                url: `${baseUrl}/store/${idStore}/box/${id}`,
                token: sessionTokenStore.token
            });
        },
        async createBox(idStore, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.box = { loading: true };
            this.box = await fetchWrapper.post({
                url: `${baseUrl}/store/${idStore}/box`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async updateBox(idStore, id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.box = { loading: true };
            this.box = await fetchWrapper.put({
                url: `${baseUrl}/store/${idStore}/box/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async deleteBox(idStore, id) {
            const sessionTokenStore = useSessionTokenStore();
            this.box = { loading: true };
            this.box = await fetchWrapper.delete({
                url: `${baseUrl}/store/${idStore}/box/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
