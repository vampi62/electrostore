import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useUsersStore = defineStore({
    id: 'users',
    state: () => ({
        users: {},
        user: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.users = { loading: true };
            this.users = await fetchWrapper.get({
                url: `${baseUrl}/user?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.user = { loading: true };
            this.user = await fetchWrapper.get({
                url: `${baseUrl}/user/${id}`,
                token: sessionTokenStore.token
            });
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.user = { loading: true };
            this.user = await fetchWrapper.post({
                url: `${baseUrl}/user`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.user = { loading: true };
            this.user = await fetchWrapper.put({
                url: `${baseUrl}/user/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.user = { loading: true };
            this.user = await fetchWrapper.delete({
                url: `${baseUrl}/user/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
