// Runtime configuration.

interface AppConfig {
	API_URL?: string;
	DEMO_MODE?: string;
	VAPID_PUBLIC_KEY?: string;
}

declare global {
	interface Window {
		__APP_CONFIG__?: AppConfig;
	}
}

const runtime: AppConfig = (typeof window !== "undefined" && window.__APP_CONFIG__) || {};

export const API_URL: string = runtime.API_URL || import.meta.env.VITE_API_URL;

export const DEMO_MODE: boolean = `${runtime.DEMO_MODE || import.meta.env.VITE_APP_DEMO_MODE}` === "true";

export const VAPID_PUBLIC_KEY: string | undefined = runtime.VAPID_PUBLIC_KEY || import.meta.env.VITE_VAPID_PUBLIC_KEY;
