import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useConfigsStore = defineStore("configs",{
	state: () => ({
		configs: {},
	}),
	actions: {
		async getConfig() {
			this.configs = { loading: true };
			this.configs = await fetchWrapper.get({
				url: `${baseUrl}/config`,
			});
		},
	},
});
