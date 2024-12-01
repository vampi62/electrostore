import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useStoresStore = defineStore({
    id: 'stores',
    state: () => ({
        stores: {},
        store: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.stores = { loading: true };
            this.stores = await fetchWrapper.get({
                url: `${baseUrl}/store?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.store = { loading: true };
            this.store = await fetchWrapper.get({
                url: `${baseUrl}/store/${id}`,
                token: sessionTokenStore.token
            });
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.store = { loading: true };
            this.store = await fetchWrapper.post({
                url: `${baseUrl}/store`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.store = { loading: true };
            this.store = await fetchWrapper.put({
                url: `${baseUrl}/store/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.store = { loading: true };
            this.store = await fetchWrapper.delete({
                url: `${baseUrl}/store/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
