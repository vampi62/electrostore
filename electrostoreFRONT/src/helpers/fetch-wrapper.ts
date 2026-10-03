import { useAuthStore } from "@/stores";
import type { TokenType } from "@/types/fetch";
import { ApiError, extractApiMessage } from "@/utils/messages";

let renewPromise: Promise<any> | null = null;

export const fetchWrapper = {
	get: request("GET"),
	post: request("POST"),
	put: request("PUT"),
	delete: request("DELETE"),
	image: image("GET"),
	stream: stream(),
};

function request(method: string) {
	return async({ url, body = null, useToken = null, contentFile = false }: { url: string; body?: any; useToken?: TokenType; contentFile?: boolean }): Promise<any> => {
		const authStore = useAuthStore();
		// if access token is expired or about to expire, try to renew it before making the request
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		} else if (useToken === "refresh" && authStore.RefreshTokenIsExpired()) {
			authStore.logout();
			throw new Error("Unable to renew token. Logging out.");
		}
		const requestOptions: { method: string; headers: Record<string, string>; body?: any } = {
			method,
			headers: authHeader(url, useToken),
		};
		if (body && !contentFile) {
			requestOptions.headers["Content-Type"] = "application/json";
			requestOptions.body = JSON.stringify(body);
		} else if (body && contentFile) {
			requestOptions.body = body;
		}
		const response = await fetch(url, requestOptions);
		const text = await response.text();
		const data = parseBody(text, response);
		if (!response.ok) {
			if (response.status === 401 && authStore.user) {
				if (!renewPromise) {
					// if the request failed with 401 and we are not already trying to renew the token, then try to renew the token
					//console.log("Token expired. Renewing token...");
					await renewToken(authStore);
					return request(method)({ url, body, useToken }); // retry the original request after renewing the token
				} else if (useToken === "refresh") {
					// if the request was using the refresh token and it failed with 401, then the refresh token is also expired, so we log out the user
					authStore.logout();
					throw new Error("Unable to renew token. Logging out.");
				}
			} else if (response.status === 403 && authStore.user) {
				throw new ApiError(extractApiMessage(data, "Access forbidden."), response.status, data);
			}
			const message = typeof data === "string" ? response.statusText : extractApiMessage(data, response.statusText);
			throw new ApiError(message || `HTTP ${response.status}`, response.status, data);
		}
		return data;
	};
}

function parseBody(text: string, response: Response): any {
	try {
		return text && JSON.parse(text);
	} catch {
		// non JSON body (proxy error page, plain text...): keep raw text for error responses
		if (response.ok) {
			throw new ApiError(response.statusText || "Invalid response", response.status, text);
		}
		return text;
	}
}

// renew token or waiting end renew
async function renewToken(authStore: any) {
	if (renewPromise) { // if there is already a renew in progress, wait for it to finish
		await renewPromise;
		return;
	}
	renewPromise = authStore.refreshLogin();
	try {
		await renewPromise;
	} catch (error) {
		authStore.logout();
		throw new Error("Unable to renew token. Logging out.");
	} finally {
		renewPromise = null;
	}
}

// download a image
function image(method: string) {
	return async({ url, useToken = null }: { url: string; useToken?: TokenType }) => {
		const authStore = useAuthStore();
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		}
		const requestOptions = {
			method,
			headers: authHeader(url, useToken),
		};
		const response = await fetch(url, requestOptions);
		if (!response.ok) {
			throw new ApiError(response.statusText || `HTTP ${response.status}`, response.status);
		}
		return await response.blob();
	};
}

// return stream mjpeg in img.src
function stream() {
	return async({ url, useToken = null }: { url: string, useToken?: TokenType }) => {
		const authStore = useAuthStore();
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		}
		return url + "?token=" + authStore.accessToken.token;
	};
}

// build header functions
function authHeader(url: string, useToken: TokenType = null): Record<string, string> {
	const authStore = useAuthStore();
	// return auth header with jwt if user is logged in and request is to the api url
	const header: Record<string, string> = {};
	const isLoggedIn = !!authStore.user;
	const isApiUrl = url.startsWith(import.meta.env.VITE_API_URL);
	if (isLoggedIn && isApiUrl && useToken) {
		if (useToken === "access") {
			header["Authorization"] = `Bearer ${authStore.accessToken.token}`;
		} else {
			header["Authorization"] = `Bearer ${authStore.refreshToken.token}`;
		}
	}
	return header;
}
