import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useProjetsStore = defineStore({
    id: 'projets',
    state: () => ({
        projets: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.projets = { loading: true };
            this.projets = await fetchWrapper.get({
                url: `${baseUrl}/projet?limit=${limit}&offset=${offset}`,
                token: token
            });
        }
    }
});
