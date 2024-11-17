import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useStoresStore = defineStore({
    id: 'stores',
    state: () => ({
        stores: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.stores = { loading: true };
            this.stores = await fetchWrapper.get(baseUrl + '/store', { 'limit': limit, 'offset': offset });
        }
    }
});
