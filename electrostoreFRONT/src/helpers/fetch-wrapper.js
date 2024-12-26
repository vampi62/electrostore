import { useAuthStore } from '@/stores';

let renewPromise = null;

export const fetchWrapper = {
    get: request('GET'),
    post: request('POST'),
    put: request('PUT'),
    delete: request('DELETE'),
    image: image('GET'),
    stream: stream('GET')
};

function request(method) {
    return async ({ url, body = null, useToken = null, contentFile = false }) => {
        const authStore = useAuthStore();
        try {
            // si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
            if (useToken === 'access' && (authStore.TokenIsExpired() || renewPromise)) {
                await renewToken(authStore);
            } else if (useToken === 'refresh' && authStore.RefreshTokenIsExpired()) {
                authStore.logout();
                throw new Error('1 Unable to renew token. Logging out.');
            }
            const requestOptions = {
                method,
                headers: authHeader(url, useToken)
            };
            if (body && !contentFile) {
                requestOptions.headers['Content-Type'] = 'application/json';
                requestOptions.body = JSON.stringify(body);
            } else if (body && contentFile) {
                requestOptions.body = body;
                for (const [key, value] of requestOptions.body.entries()) {
                    console.log(key, value);
                }
            }
            const response = await fetch(url, requestOptions);
            const text = await response.text();
            const totalCount = response.headers.get('X-Total-Count');
            const data = text && JSON.parse(text);
            if (!response.ok) {
                if (response.status === 401 && authStore.user) {
                    if (!renewPromise) {
                        // Si c'est la première tentative de renouvellement, on réessaie
                        console.log('Token expired. Renewing token...');
                        await renewToken(authStore);
                        return request(method)({ url, body, useToken }); // Réessayer la requête après renouvellement
                    } else {
                        if (useToken === 'refresh') {
                            // Si la requete cherche a renouveler le token echoue alors on deconnecte le client
                            authStore.logout();
                            throw new Error('2 Unable to renew token. Logging out.');
                        }
                    }
                } else if (response.status === 403 && authStore.user) {
                    throw new Error('Access forbidden.');
                }
                const error = (data && data.errors) || response.statusText;
                throw new Error(error);
            }
            if (totalCount) {
                return { 'data': data, 'count': totalCount };
            }
            return data;
        } catch (error) {
            console.error('Request failed:', error);
            throw error;
        }
    }
}

// renew token or waiting end renew
async function renewToken(authStore) {
    if (renewPromise) { // une requete est deja lancer on attend la fin
        console.log('Waiting for token renewal to complete...');
        await renewPromise;
        return;
    }
    renewPromise = authStore.refreshLogin();
    try {
        await renewPromise;
    } catch (error) {
        console.error('Token renewal failed:', error);
        authStore.logout();
        throw new Error('Unable to renew token. Logging out.');
    } finally {
        renewPromise = null;
    }
}

// download a image
function image(method) {
    return async ({ url, useToken = null }) => {
        const authStore = useAuthStore();
        // si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
        if (useToken === 'access' && (authStore.TokenIsExpired() || renewPromise)) {
            await renewToken(authStore);
        }
        const requestOptions = {
            method,
            headers: authHeader(url, useToken)
        };
        const response = await fetch(url, requestOptions);
        if (!response.ok) {
            return Promise.reject(response.statusText);
        }
        return await response.blob();
    }
}

// return stream mjpeg in img.src
function stream() {
    return async ({ url, useToken = null }) => {
        const authStore = useAuthStore();
        // si le token est sur le point d'expirer ou qu'une requete de renouvellement est lancer
        if (useToken === 'access' && (authStore.TokenIsExpired() || renewPromise)) {
            await renewToken(authStore);
        }
        return url + '?token=' + authStore.accessToken.token;
    }
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
            header['Authorization'] = `Bearer ${authStore.accessToken.token}`;
        } else {
            header['Authorization'] = `Bearer ${authStore.refreshToken.token}`;
        }
    }
    header['Access-Control-Allow-Origin'] = '*';
    return header;
}