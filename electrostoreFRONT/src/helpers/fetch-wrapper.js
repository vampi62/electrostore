import { useAuthStore } from "@/stores";

let renewPromise = null;

export const fetchWrapper = {
	get: request("GET"),
	post: request("POST"),
	put: request("PUT"),
	delete: request("DELETE"),
	image: image("GET"),
	stream: stream(),
};

function request(method) {
	return async({ url, body = null, useToken = null, contentFile = false }) => {
		const authStore = useAuthStore();
		// if access token is expired or about to expire, try to renew it before making the request
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		} else if (useToken === "refresh" && authStore.RefreshTokenIsExpired()) {
			authStore.logout();
			throw new Error("Unable to renew token. Logging out.");
		}
		const requestOptions = {
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
		const data = text && JSON.parse(text);
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
				throw new Error("Access forbidden.");
			}
			const error = data?.errors || response.statusText;
			throw new Error(error);
		}
		return data;
	};
}

// renew token or waiting end renew
async function renewToken(authStore) {
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
function image(method) {
	return async({ url, useToken = null }) => {
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
			return Promise.reject(new Error(response.statusText));
		}
		return await response.blob();
	};
}

// return stream mjpeg in img.src
function stream() {
	return async({ url, useToken = null }) => {
		const authStore = useAuthStore();
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		}
		return url + "?token=" + authStore.accessToken.token;
	};
}

// build header functions
function authHeader(url, useToken = null) {
	const authStore = useAuthStore();
	// return auth header with jwt if user is logged in and request is to the api url
	const header = {};
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