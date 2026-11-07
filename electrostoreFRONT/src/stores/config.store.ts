import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";
import { API_URL, DEMO_MODE } from "@/helpers/appConfig";

const baseUrl = `${API_URL}`;

const demoMode = DEMO_MODE;

import type { components } from "@/types/api";
type ReadConfig = components["schemas"]["ReadConfig"];
type ReadStatusDto = components["schemas"]["ReadStatusDto"];

interface Configs extends ReadConfig {
	loading: boolean;
}

interface Status extends ReadStatusDto {
	loading: boolean;
}

// request of the config in progress (or done), shared by the app and the router guard
let configRequest: Promise<void> | null = null;

export const useConfigsStore = defineStore("configs",{
	state: () => ({
		configs: {} as Configs,
		status: {} as Status,
		defaultsConfig: {
			"demo_mode": demoMode,
			"max_length_url": 150,
			"max_length_comment": 455,
			"max_length_description": 500,
			"max_length_name": 50,
			"max_length_type": 50,
			"max_length_email": 100,
			"max_length_ip": 50,
			"max_length_reason": 50,
			"max_length_status": 50,
			"max_size_document_in_mb": 5,
			"max_size_image_in_mb": 5,
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
			"max_size_audio_in_mb": 25,
			"allowed_audio_mime_types": [
				"audio/mpeg",
				"audio/mp3",
				"audio/wav",
				"audio/x-wav",
				"audio/webm",
				"audio/ogg",
				"audio/mp4",
				"audio/m4a",
			],
			"allowed_audio_extensions": [
				".mp3",
				".wav",
				".webm",
				".ogg",
				".m4a",
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
			"llm_status": "unknown",
			"stt_status": "unknown",
			"notif_status": "unknown",
			"notif_smtp": false,
			"notif_web_push": false,
			"cron_status": "unknown",
			"worker_status": "unknown",
			"external_services": {
			},
		},
	}),
	actions: {
		async getConfig() {
			this.configs.loading = true;
			configRequest = fetchWrapper.get<ReadConfig>({
				url: `${baseUrl}/config`,
			}).then((config) => {
				this.configs = { ...config, loading: false };
			});
			try {
				await configRequest;
			} catch (error) {
				// the next call (guard, refresh button) will try again
				configRequest = null;
				throw error;
			}
		},
		// waits for the config (fetched only once), the defaults apply when the api can't be reached
		async ensureConfig() {
			try {
				await (configRequest ?? this.getConfig());
			} catch {
				// the pages use the default config
			}
		},
		async getHealth() {
			this.status.loading = true;
			const status = await fetchWrapper.get<ReadStatusDto>({
				url: `${baseUrl}/status`,
			});
			this.status = { ...status, loading: false };
		},
	},
	getters: {
		isPasswordLoginEnabled: (state) => state.configs.password_login_enabled !== false,
		isSignUpEnabled: (state) => state.configs.sign_up_enabled !== false,
		getConfigByKey: (state) => (key: string) => {
			const configs = state.configs as unknown as Record<string, unknown>;
			if (configs[key]) {
				return configs[key];
			}
			const defaults = state.defaultsConfig as unknown as Record<string, unknown>;
			if (defaults[key]) {
				return defaults[key];
			}
			return null;
		},
		// numeric configs (max lengths and sizes), 0 when the config is unknown
		getConfigNumber: (state) => (key: string): number => {
			const configs = state.configs as unknown as Record<string, unknown>;
			const defaults = state.defaultsConfig as unknown as Record<string, unknown>;
			return Number(configs[key] || defaults[key] || 0);
		},
		getStatusByKey: (state) => (key: string) => {
			const status = state.status as unknown as Record<string, unknown>;
			if (status[key]) {
				return status[key];
			}
			const defaults = state.defaultsStatus as unknown as Record<string, unknown>;
			if (defaults[key]) {
				return defaults[key];
			}
			return null;
		},
	},
});
