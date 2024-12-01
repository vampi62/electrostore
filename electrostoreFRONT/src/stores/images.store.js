import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useImagesStore = defineStore({
    id: 'images',
    state: () => ({
        images: {}
    }),
    actions: {
        async showImageById(id_item, id_img) {
            const sessionTokenStore = useSessionTokenStore();
            const response = await fetchWrapper.image({
                url: `${baseUrl}/item/${id_item}/img/${id_img}/show`,
                token: sessionTokenStore.token
            });
            const url = URL.createObjectURL(response);
            this.images[id_img] = url;
        }
    }
});
