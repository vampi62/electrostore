import { useNotification } from "@/helpers/notification.js";
import { useAuthStore } from "@/stores";
const { addNotification } = useNotification();

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
		try {
			// si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
			if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
				await renewToken(authStore);
			} else if (useToken === "refresh" && authStore.RefreshTokenIsExpired()) {
				authStore.logout();
				throw new Error("1 Unable to renew token. Logging out.");
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
			const totalCount = response.headers.get("X-Total-Count");
			const data = text && JSON.parse(text);
			if (!response.ok) {
				if (response.status === 401 && authStore.user) {
					if (!renewPromise) {
						// Si c'est la première tentative de renouvellement, on réessaie
						console.log("Token expired. Renewing token...");
						await renewToken(authStore);
						return request(method)({ url, body, useToken }); // Réessayer la requête après renouvellement
					} else if (useToken === "refresh") {
						// Si la requete cherche a renouveler le token echoue alors on deconnecte le client
						authStore.logout();
						throw new Error("2 Unable to renew token. Logging out.");
					}
				} else if (response.status === 403 && authStore.user) {
					throw new Error("Access forbidden.");
				}
				const error = data?.errors || response.statusText;
				throw new Error(error);
			}
			if (totalCount) {
				return { "data": data, "count": totalCount };
			}
			return data;
		} catch (error) {
			addNotification({
				type: "error",
				message: `Error in fetch request: ${error.message}`,
			});
			throw error;
		}
	};
}

// renew token or waiting end renew
async function renewToken(authStore) {
	if (renewPromise) { // une requete est deja lancer on attend la fin
		await renewPromise;
		return;
	}
	renewPromise = authStore.refreshLogin();
	try {
		await renewPromise;
	} catch (error) {
		addNotification({
			type: "error",
			message: `Error renewing token: ${error.message}`,
		});
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
		// si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
		if (useToken === "access" && (authStore.TokenIsExpired() || renewPromise)) {
			await renewToken(authStore);
		}
		const requestOptions = {
			method,
			headers: authHeader(url, useToken),
		};
		const response = await fetch(url, requestOptions);
		if (!response.ok) {
			addNotification({
				type: "error",
				message: `Error downloading image: ${response.statusText}`,
			});
			return Promise.reject(new Error(response.statusText));
		}
		return await response.blob();
	};
}

// return stream mjpeg in img.src
function stream() {
	return async({ url, useToken = null }) => {
		const authStore = useAuthStore();
		// si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
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
			// wait if a renew token is running
			header["Authorization"] = `Bearer ${authStore.accessToken.token}`;
		} else {
			header["Authorization"] = `Bearer ${authStore.refreshToken.token}`;
		}
	}
	header["Access-Control-Allow-Origin"] = "*";
	return header;
}