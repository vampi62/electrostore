import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCommandesStore = defineStore({
    id: 'commandes',
    state: () => ({
        commandes: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.commandes = { loading: true };
            this.commandes = await fetchWrapper.get({
                url: `${baseUrl}/command?limit=${limit}&offset=${offset}`,
                token: token
            });
        }
    }
});
