import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useConfigsStore = defineStore({
    id: 'configs',
    state: () => ({
        configs: {},
        sidebar: false
    }),
    actions: {
        async getConfig() {
            this.configs = { loading: true };
            this.configs = await fetchWrapper.get(baseUrl + '/config');
        },
        toggleSidebar() {
            this.sidebar = !this.sidebar;
        },
        reduceSidebar() {
            this.sidebar = false;
        }
    }
});
