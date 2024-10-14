import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useCamerasStore = defineStore({
    id: 'cameras',
    state: () => ({
        cameras: {}
    }),
    actions: {
        async getAll(limit=100, offset=0) {
            this.cameras = { loading: true };
            this.cameras = await fetchWrapper.get(baseUrl + '/camera', {'limit': limit, 'offset': offset});
        }
    }
});
