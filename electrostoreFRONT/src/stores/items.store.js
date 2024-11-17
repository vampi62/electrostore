import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useItemsStore = defineStore({
    id: 'items',
    state: () => ({
        items: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.items = { loading: true };
            this.items = await fetchWrapper.get(baseUrl + '/item' + '?limit=' + limit + '&offset=' + offset);
        },
        async getImages(id) {
            return await fetchWrapper.image(baseUrl + '/img/' + id + '/show');
        }
    }
});
