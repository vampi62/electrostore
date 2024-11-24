import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const token = localStorage.getItem('token');

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useImagesStore = defineStore({
    id: 'images',
    state: () => ({
        images: {}
    }),
    actions: {
        async getImages(id_item, id_img) {
            console.log('getImages', id);
            const response = await fetchWrapper.image({
                url: `${baseUrl}/item/${id_item}/img/${id_img}/show`,
                token: token
            });
            const url = URL.createObjectURL(response);
            this.images[id_img] = url;
        }
    }
});
