import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const demoMode = `${import.meta.env.APP_DEMO_MODE}` === "true";

export const useConfigsStore = defineStore("configs",{
	state: () => ({
		configs: {},
		defaultsConfig: {
			"smtp_enabled": false,
			"mqtt_connected": false,
			"ia_connected": false,
			"max_length_url": 150,
			"max_length_commentaire": 455,
			"max_length_description": 500,
			"max_length_name": 50,
			"max_length_type": 50,
			"max_length_email": 100,
			"max_length_ip": 50,
			"max_length_reason": 50,
			"max_length_status": 50,
			"max_size_document_in_mb": 5,
		},
	}),
	actions: {
		async getConfig() {
			this.configs.loading = true;
			this.configs = await fetchWrapper.get({
				url: `${baseUrl}/config`,
			});
		},
	},
	getters: {
		getConfigByKey: (state) => (key) => {
			if (state.configs[key]) {
				return state.configs[key];
			}
			if (state.defaultsConfig[key]) {
				return state.defaultsConfig[key];
			}
			return null;
		},
	},
});
