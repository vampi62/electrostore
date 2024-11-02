import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useUsersStore = defineStore({
    id: 'users',
    state: () => ({
        users: {},
        user: {}
    }),
    actions: {
        async getAll(limit=100, offset=0) {
            this.users = { loading: true };
            this.users = await fetchWrapper.get(baseUrl + '/user' + '?limit=' + limit + '&offset=' + offset);
        },
        async getById(id) {
            this.user = { loading: true };
            this.user = await fetchWrapper.get(baseUrl + `/user/${id}`);
        },
        async create(params) {
            this.user = { loading: true };
            this.user = await fetchWrapper.post(baseUrl + '/user', params);
        },
        async update(id, params) {
            this.user = { loading: true };
            this.user = await fetchWrapper.put(baseUrl + `/user/${id}`, params);
        },
        async delete(id) {
            this.user = { loading: true };
            this.user = await fetchWrapper.delete(baseUrl + `/user/${id}`);
        }
    }
});
