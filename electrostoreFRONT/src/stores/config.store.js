import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";

import { getExtension } from "@/utils/mimeTypes.js";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

const demoMode = `${import.meta.env.VITE_APP_DEMO_MODE}` === "true";

export const useConfigsStore = defineStore("configs",{
	state: () => ({
		configs: {},
		defaultsConfig: {
			"smtp_enabled": false,
			"mqtt_connected": false,
			"ia_service_status": false,
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
				"application/octet-stream",
				"image/png",
				"image/webp",
				"image/jpg",
				"image/jpeg",
				"image/gif",
				"image/bmp",
			],
		},
		imageExtAllowed: [],
		documentExtAllowed: [],
	}),
	actions: {
		async getConfig() {
			this.configs.loading = true;
			this.configs = await fetchWrapper.get({
				url: `${baseUrl}/config`,
			});
			this.convertMimeTypesToExtensions();
			this.configs.loading = false;
		},
		convertMimeTypesToExtensions() {
			const imageMimeTypes = this.getConfigByKey("allowed_image_mime_types");
			const documentMimeTypes = this.getConfigByKey("allowed_document_mime_types");
			for (const mimeType of imageMimeTypes) {
				const ext = "." + getExtension(mimeType);
				if (!this.imageExtAllowed.includes(ext)) {
					this.imageExtAllowed.push(ext);
				}
			}
			for (const mimeType of documentMimeTypes) {
				const ext = "." + getExtension(mimeType);
				if (!this.documentExtAllowed.includes(ext)) {
					this.documentExtAllowed.push(ext);
				}
			}
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
