import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useItemsStore = defineStore({
    id: 'items',
    state: () => ({
        items: {},
        images: {},
        item: {},
        itemImages: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            const sessionTokenStore = useSessionTokenStore();
            this.items = { loading: true };
            this.items = await fetchWrapper.get({
                url: `${baseUrl}/item?limit=${limit}&offset=${offset}`,
                token: sessionTokenStore.token
            });
            for (const item of this.items) {
                if (item.id_img && !this.images[item.id_img]) {
                    this.showImageById(item.id_item ,item.id_img);
                }
            }
        },
        async showImageById(id_item, id_img) {
            const sessionTokenStore = useSessionTokenStore();
            const response = await fetchWrapper.image({
                url: `${baseUrl}/item/${id_item}/img/${id_img}/show`,
                token: sessionTokenStore.token
            });
            const url = URL.createObjectURL(response);
            this.images[id_img] = url;
        },
        async getById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.item = { loading: true };
            this.item = await fetchWrapper.get({
                url: `${baseUrl}/item/${id}`,
                token: sessionTokenStore.token
            });
        },
        async getImageListById(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.itemImages = { loading: true };
            this.itemImages = await fetchWrapper.get({
                url: `${baseUrl}/item/${id}/img`,
                token: sessionTokenStore.token
            });
            for (const img of listeImages) {
                this.showImageById(id ,img.id_img);
            }
        },
        async create(params) {
            const sessionTokenStore = useSessionTokenStore();
            this.item = { loading: true };
            this.item = await fetchWrapper.post({
                url: `${baseUrl}/item`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async update(id, params) {
            const sessionTokenStore = useSessionTokenStore();
            this.item = { loading: true };
            this.item = await fetchWrapper.put({
                url: `${baseUrl}/item/${id}`,
                token: sessionTokenStore.token,
                body: params
            });
        },
        async delete(id) {
            const sessionTokenStore = useSessionTokenStore();
            this.item = { loading: true };
            this.item = await fetchWrapper.delete({
                url: `${baseUrl}/item/${id}`,
                token: sessionTokenStore.token
            });
        }
    }
});
