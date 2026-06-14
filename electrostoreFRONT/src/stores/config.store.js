import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { getExtension } from "@/utils/mimeTypes.js";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const demoMode = `${import.meta.env.VITE_APP_DEMO_MODE}` === "true";

export const useConfigsStore = defineStore("configs",{
	state: () => ({
		configs: {},
		status: {},
		defaultsConfig: {
			"demo_mode": demoMode,
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
			"sso_available_providers": [],// e.g : [{"provider":"authentik","display_name":"Authentik","icon_url":"https://example.com/icon.png"}]
			"allowed_image_mime_types": [
				"image/png",
				"image/webp",
				"image/jpg",
				"image/jpeg",
				"image/gif",
				"image/bmp",
			],
			"allowed_image_extensions": [
				".png",
				".webp",
				".jpg",
				".jpeg",
				".gif",
				".bmp",
			],
			"allowed_document_mime_types": [
				"application/pdf",
				"application/msword",
				"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
				"application/vnd.ms-excel",
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"application/vnd.ms-powerpoint",
				"application/vnd.openxmlformats-officedocument.presentationml.presentation",
				"text/plain",
				"application/zip",
				"application/x-rar-compressed",
				"image/png",
				"image/webp",
				"image/jpg",
				"image/jpeg",
				"image/gif",
				"image/bmp",
			],
			"allowed_document_extensions": [
				".pdf",
				".doc",
				".docx",
				".xls",
				".xlsx",
				".ppt",
				".pptx",
				".txt",
				".zip",
				".rar",
				".png",
				".webp",
				".jpg",
				".jpeg",
				".gif",
				".bmp",
			],
		},
		defaultsStatus: {
			"api_status": "unknown",
			"db_connected": false,
			"mqtt_connected": false,
			"kafka_connected": false,
			"ia_status": "unknown",
			"ia_training_in_progress": 0,
			"notif_status": "unknown",
			"notif_smtp": false,
			"notif_webPush": false,
			"cron_status": "unknown",
			"worker_status": "unknown",
		},
	}),
	actions: {
		async getConfig() {
			this.configs.loading = true;
			this.configs = await fetchWrapper.get({
				url: `${baseUrl}/config`,
			});
		},
		async getHealth() {
			this.status = await fetchWrapper.get({
				url: `${baseUrl}/status`,
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
		getStatusByKey: (state) => (key) => {
			if (state.status[key]) {
				return state.status[key];
			}
			if (state.defaultsStatus[key]) {
				return state.defaultsStatus[key];
			}
			return null;
		},
	},
});
