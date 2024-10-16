import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useBoxsStore = defineStore({
    id: 'boxs',
    state: () => ({
        boxs: {}
    }),
    actions: {
        async getAll(limit=100, offset=0) {
            this.boxs = { loading: true };
            this.boxs = await fetchWrapper.get(baseUrl + '/box', {'limit': limit, 'offset': offset});
        }
    }
});
