import { defineStore } from 'pinia';

import { fetchWrapper } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useIasStore = defineStore({
    id: 'ias',
    state: () => ({
        loading: true,
        TotalCount: 0,
        ias: {},
        iaEdition: {}
    }),
    actions: {
        async getIaByList(idResearch = []) {
            this.loading = true;
            const idResearchString = idResearch.join(',');
            let newIaList = await fetchWrapper.get({
                url: `${baseUrl}/ia?&idResearch=${idResearchString}`,
                useToken: "access"
            });
            for (const ia of newIaList['data']) {
                this.ias[ia.id_ia] = ia;
            }
            this.TotalCount = newIaList['count'];
            this.loading = false;
        },
        async getIaByInterval(limit = 100, offset = 0) {
            this.loading = true;
            let newIaList = await fetchWrapper.get({
                url: `${baseUrl}/ia?limit=${limit}&offset=${offset}`,
                useToken: "access"
            });
            for (const ia of newIaList['data']) {
                this.ias[ia.id_ia] = ia;
            }
            this.TotalCount = newIaList['count'];
            this.loading = false;
        },
        async getIaById(id) {
            this.ias[id] = { loading: true };
            this.ias[id] = await fetchWrapper.get({
                url: `${baseUrl}/ia/${id}`,
                useToken: "access"
            });
        },
        async createIa(params) {
            this.iaEdition.loading = true;
            this.iaEdition = await fetchWrapper.post({
                url: `${baseUrl}/ia`,
                useToken: "access",
                body: params
            });
            this.ias[this.iaEdition.id_ia] = this.iaEdition;
        },
        async updateIa(id, params) {
            this.iaEdition.loading = true;
            this.iaEdition = await fetchWrapper.put({
                url: `${baseUrl}/ia/${id}`,
                useToken: "access",
                body: params
            });
            this.ias[id] = params;
        },
        async deleteIa(id) {
            this.iaEdition.loading = true;
            this.iaEdition = await fetchWrapper.delete({
                url: `${baseUrl}/ia/${id}`,
                useToken: "access"
            });
            delete this.ias[id];
        }
    }
});
