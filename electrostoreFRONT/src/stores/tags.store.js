import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useTagsStore = defineStore({
    id: 'tags',
    state: () => ({
        tags: {},
        tag: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.tags = { loading: true };
            this.tags = await fetchWrapper.get({
                url: `${baseUrl}/tag?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.tag = { loading: true };
            this.tag = await fetchWrapper.get({
                url: `${baseUrl}/tag/${id}`,
                token: sessionTokenStore.token
            });
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.tag = { loading: true };
            this.tag = await fetchWrapper.post({
                url: `${baseUrl}/tag`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.tag = { loading: true };
            this.tag = await fetchWrapper.put({
                url: `${baseUrl}/tag/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.tag = { loading: true };
            this.tag = await fetchWrapper.delete({
                url: `${baseUrl}/tag/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
