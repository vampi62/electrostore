import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useIasStore = defineStore({
    id: 'ias',
    state: () => ({
        ias: {}
    }),
    actions: {
        async getAll(limit=100, offset=0) {
            this.ias = { loading: true };
            this.ias = await fetchWrapper.get({
                url: `${baseUrl}/ia?limit=${limit}&offset=${offset}`,
                token: token
            });
        }
    }
});
