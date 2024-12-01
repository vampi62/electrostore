import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useIasStore = defineStore({
    id: 'ias',
    state: () => ({
        ias: {},
        ia: {}
    }),
    actions: {
        async getAll(limit=100, offset=0) {
            const sessionTokenStore = useSessionTokenStore();
            this.ias = { loading: true };
            this.ias = await fetchWrapper.get({
                url: `${baseUrl}/ia?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.ia = { loading: true };
            this.ia = await fetchWrapper.get({
                url: `${baseUrl}/ia/${id}`,
                token: sessionTokenStore.token
            });
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.ia = { loading: true };
            this.ia = await fetchWrapper.post({
                url: `${baseUrl}/ia`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.ia = { loading: true };
            this.ia = await fetchWrapper.put({
                url: `${baseUrl}/ia/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.ia = { loading: true };
            this.ia = await fetchWrapper.delete({
                url: `${baseUrl}/ia/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
