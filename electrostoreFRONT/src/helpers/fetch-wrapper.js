import { useAuthStore } from '@/stores';

export const fetchWrapper = {
    get: request('GET'),
    post: request('POST'),
    put: request('PUT'),
    delete: request('DELETE'),
    image: image('GET')
};

function request(method) {
    return (url, body) => {
        const requestOptions = {
            method,
            headers: authHeader(url)
        };
        if (body) {
            requestOptions.headers['Content-Type'] = 'application/json';
            requestOptions.body = JSON.stringify(body);
        }
        return fetch(url, requestOptions).then(handleResponse);
    }
}

function image(method) {
    // download a image
    return (url) => {
        const requestOptions = {
            method,
            headers: authHeader(url)
        };
        return fetch(url, requestOptions).then(response => {
            if (!response.ok) {
                return Promise.reject(response.statusText);
            }
            return response.blob();
        });
    }
}

// helper functions

function authHeader(url) {
    // return auth header with jwt if user is logged in and request is to the api url
    const header = {};
    const { user } = useAuthStore();
    const isLoggedIn = !!user?.token;
    const isApiUrl = url.startsWith(import.meta.env.VITE_API_URL);
    if (isLoggedIn && isApiUrl) {
        header['Authorization'] = `Bearer ${user.token}`;
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
