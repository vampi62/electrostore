import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useItemsStore = defineStore({
    id: 'items',
    state: () => ({
        items: {},
        images: {}
    }),
    actions: {
        async getAll(limit = 100, offset = 0) {
            this.items = { loading: true };
            this.items = await fetchWrapper.get({
                url: `${baseUrl}/item?limit=${limit}&offset=${offset}`,
                token: token
            });
            for (const item of this.items) {
                if (item.id_img && !this.images[item.id_img]) {
                    this.getImages(item.id_item ,item.id_img);
                }
            }
        },
        async getImages(id_item, id_img) {
            console.log('getImages', id_img);
            const response = await fetchWrapper.image({
                url: `${baseUrl}/item/${id_item}/img/${id_img}/show`,
                token: token
            });
            const url = URL.createObjectURL(response);
            this.images[id_img] = url;
        }
    }
});
