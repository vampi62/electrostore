import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCommandesStore = defineStore({
    id: 'commandes',
    state: () => ({
        commandes: {},
        command: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.commandes = { loading: true };
            this.commandes = await fetchWrapper.get({
                url: `${baseUrl}/command?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.command = { loading: true };
            this.command = await fetchWrapper.get({
                url: `${baseUrl}/command/${id}`,
                token: sessionTokenStore.token
            });
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.command = { loading: true };
            this.command = await fetchWrapper.post({
                url: `${baseUrl}/command`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.command = { loading: true };
            this.command = await fetchWrapper.put({
                url: `${baseUrl}/command/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.command = { loading: true };
            this.command = await fetchWrapper.delete({
                url: `${baseUrl}/command/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
