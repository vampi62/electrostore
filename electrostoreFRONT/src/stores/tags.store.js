import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useTagsStore = defineStore({
    id: 'tags',
    state: () => ({
        tags: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.tags = { loading: true };
            this.tags = await fetchWrapper.get(baseUrl + '/tag', { 'limit': limit, 'offset': offset });
        }
    }
});
