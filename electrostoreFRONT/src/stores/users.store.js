import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useUsersStore = defineStore({
    id: 'users',
    state: () => ({
        users: {},
        user: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.users = { loading: true };
            this.users = await fetchWrapper.get({
                url: `${baseUrl}/user?limit=${limit}&offset=${offset}`,
                token: token
            });
        },
        async getById(id) {
            this.user = { loading: true };
            this.user = await fetchWrapper.get({
                url: `${baseUrl}/user/${id}`,
                token: token
            });
        },
        async create(params) {
            this.user = { loading: true };
            this.user = await fetchWrapper.post({
                url: `${baseUrl}/user`,
                token: token,
                body: params
            });
        },
        async update(id, params) {
            this.user = { loading: true };
            this.user = await fetchWrapper.put({
                url: `${baseUrl}/user${id}`,
                token: token,
                body: params
            });
        },
        async delete(id) {
            this.user = { loading: true };
            this.user = await fetchWrapper.delete({
                url: `${baseUrl}/user${id}`,
                token: token
            });
        }
    }
});
