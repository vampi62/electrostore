import { useAuthStore } from "@/stores";
import { ApiError, extractApiMessage } from "@/utils/messages";
import type { TokenType, RequestParams } from "@/types/fetch";
import type { StoreGeneric } from "pinia";

const LOGOUT_MESSAGE = "Unable to renew token. Logging out.";

let renewPromise: Promise<void> | null = null;

export const fetchWrapper = {
	get: request("GET"),
	post: request("POST"),
	put: request("PUT"),
	delete: request("DELETE"),
	image: image("GET"),
	stream: stream(),
};

/* ------------------------------------------------------------------ */
/* Token renewal management                                           */
/* ------------------------------------------------------------------ */

function isRenewing(): boolean {
	return renewPromise !== null;
}

// Start the renewal (only once) or join the one already in progress.
// All callers get the same promise, so they share the same result / error.
function renewToken(authStore: StoreGeneric): Promise<void> {
	renewPromise ??= doRenew(authStore).finally(() => {
		renewPromise = null;
	});
	return renewPromise;
}

async function doRenew(authStore: StoreGeneric): Promise<void> {
	try {
		await authStore.refreshLogin();
	} catch (error) {
		authStore.logout();
		throw new Error(LOGOUT_MESSAGE, { cause: error });
	}
}

// Call before any request using the access token:
// waits for the renewal in progress, or starts one if the token is expired
async function ensureValidAccessToken(authStore: StoreGeneric, useToken: TokenType): Promise<void> {
	if (useToken === "access" && (isRenewing() || authStore.TokenIsExpired())) {
		await renewToken(authStore);
	}
}

/* ------------------------------------------------------------------ */
/* JSON requests                                                      */
/* ------------------------------------------------------------------ */

function request(method: string) {
	const send = async<T>(params: RequestParams, hasRetried: boolean): Promise<T> => {
		const { url, body = null, useToken = null, contentFile = false } = params;
		const authStore = useAuthStore();

		if (useToken === "refresh" && authStore.RefreshTokenIsExpired()) {
			authStore.logout();
			throw new Error(LOGOUT_MESSAGE);
		}
		await ensureValidAccessToken(authStore, useToken);

		const requestOptions: { method: string; headers: Record<string, string>; body?: BodyInit } = {
			method,
			headers: authHeader(url, useToken),
		};
		if (body !== null) {
			if (contentFile) {
				requestOptions.body = body as BodyInit;
			} else {
				requestOptions.headers["Content-Type"] = "application/json";
				requestOptions.body = JSON.stringify(body);
			}
		}

		const response = await fetch(url, requestOptions);
		const data = parseBody(await response.text(), response);

		if (response.ok) {
			return data as T;
		}

		if (response.status === 401 && authStore.user) {
			// the refresh token itself is rejected: session is over
			if (useToken === "refresh") {
				authStore.logout();
				throw new Error(LOGOUT_MESSAGE);
			}
			// access token rejected: renew it (or join the renewal in progress),
			// then replay the request ONCE with the same params (contentFile included)
			if (useToken === "access" && !hasRetried) {
				await renewToken(authStore);
				return send(params, true);
			}
		} else if (response.status === 403 && authStore.user) {
			throw new ApiError(extractApiMessage(data, "Access forbidden."), response.status, data);
		}

		const message = typeof data === "string" ? response.statusText : extractApiMessage(data, response.statusText);
		throw new ApiError(message || `HTTP ${response.status}`, response.status, data);
	};

	return <T = unknown>(params: RequestParams): Promise<T> => send<T>(params, false);
}

function parseBody(text: string, response: Response): unknown {
	if (!text) {
		return text;
	}
	try {
		return JSON.parse(text) as unknown;
	} catch {
		// non-JSON body (proxy error page, plain text...): keep the raw text for error responses
		if (response.ok) {
			throw new ApiError(response.statusText || "Invalid response", response.status, text);
		}
		return text;
	}
}

/* ------------------------------------------------------------------ */
/* Image / stream                                                     */
/* ------------------------------------------------------------------ */

// download an image
function image(method: string) {
	return async({ url, useToken = null }: { url: string; useToken?: TokenType }): Promise<Blob> => {
		const authStore = useAuthStore();
		await ensureValidAccessToken(authStore, useToken);

		const response = await fetch(url, { method, headers: authHeader(url, useToken) });
		if (!response.ok) {
			throw new ApiError(response.statusText || `HTTP ${response.status}`, response.status);
		}
		return await response.blob();
	};
}

// return the mjpeg stream url to be used in img.src
function stream() {
	return async({ url, useToken = null }: { url: string; useToken?: TokenType }): Promise<string> => {
		const authStore = useAuthStore();
		await ensureValidAccessToken(authStore, useToken);
		return `${url}?token=${authStore.accessToken.token}`;
	};
}

/* ------------------------------------------------------------------ */
/* Headers                                                            */
/* ------------------------------------------------------------------ */

function authHeader(url: string, useToken: TokenType = null): Record<string, string> {
	const authStore = useAuthStore();
	const header: Record<string, string> = {};
	const isLoggedIn = !!authStore.user;
	const isApiUrl = url.startsWith(import.meta.env.VITE_API_URL);

	if (isLoggedIn && isApiUrl && useToken) {
		const token = useToken === "access" ? authStore.accessToken.token : authStore.refreshToken.token;
		header["Authorization"] = `Bearer ${token}`;
	}
	return header;
}
