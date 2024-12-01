import { useAuthStore } from '@/stores';

import { ref } from 'vue';

export const fetchWrapper = {
    get: request('GET'),
    post: request('POST'),
    put: request('PUT'),
    delete: request('DELETE'),
    image: image('GET'),
    stream: stream('GET')
};

function request(method) {
    return async ({ url, body = null, token = null }) => {
        const requestOptions = {
            method,
            headers: authHeader(url, token)
        };
        if (body) {
            requestOptions.headers['Content-Type'] = 'application/json';
            requestOptions.body = JSON.stringify(body);
        }
        const response = await fetch(url, requestOptions);
        return handleResponse(response);
    }
}

function image(method) {
    // download a image
    return async ({ url, token = null }) => {
        const requestOptions = {
            method,
            headers: authHeader(url, token)
        };
        const response = await fetch(url, requestOptions);
        if (!response.ok) {
            return Promise.reject(response.statusText);
        }
        return await response.blob();
    }
}

function stream(method) {
    // download a stream
    return ({ url, token = null }) => {
        return url + '?token=' + token;
    }
}

// helper functions
function authHeader(url, token = null) {
    // return auth header with jwt if user is logged in and request is to the api url
    const header = {};
    const { user } = useAuthStore();
    const isLoggedIn = !!user;
    const isApiUrl = url.startsWith(import.meta.env.VITE_API_URL);
    if (isLoggedIn && isApiUrl && token) {
        header['Authorization'] = `Bearer ${token}`;
    }
    header['Access-Control-Allow-Origin'] = '*';
    return header;
}

function handleResponse(response) {
    return response.text().then(text => {
        const data = text && JSON.parse(text);
        if (!response.ok) {
            const { user, logout } = useAuthStore();
            if (response.status == 401 && user) {
                // auto logout if 401 Unauthorized
                //refresh token
            } else if (response.status == 403 && user) {
                // auto logout if 403 Forbidden
                logout();
            }
            const error = (data && data.errors) || response.statusText;
            return Promise.reject(error);
        }

        return data;
    });
}    
