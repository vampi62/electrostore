import { defineStore } from 'pinia';

import { fetchWrapper, router } from '@/helpers';



const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useAuthStore = defineStore('auth',{
    state: () => ({
        // initialize state from local storage to enable user to stay logged in
        user: JSON.parse(localStorage.getItem('user')) || null,
        accessToken: JSON.parse(localStorage.getItem('accessToken')) || null,
        refreshToken: JSON.parse(localStorage.getItem('refreshToken')) || null,
        returnUrl: null
    }),
    actions: {
        setToken(user, accessToken, refreshToken) {
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;
            this.user = user;
            localStorage.setItem('accessToken', JSON.stringify(accessToken));
            localStorage.setItem('refreshToken', JSON.stringify(refreshToken));
            localStorage.setItem('user', JSON.stringify(user));
        },
        clearToken() {
            this.accessToken = null;
            this.refreshToken = null;
            this.user = null;
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('user');
        },
        TokenIsExpired() {
            // if date expire is less than current date + 5 minutes
            if (new Date(this.accessToken.date_expire).getTime() < new Date().getTime() + 5 * 60000) {
                console.log('Token is expired');
                return true;
            }
            return false;
        },
        RefreshTokenIsExpired() {
            // if date expire is less than current date
            if (new Date(this.refreshToken.date_expire).getTime() < new Date().getTime()) {
                console.log('Refresh Token is expired');
                return true;
            }
            return false;
        },
        async login(email, password) {
            const request = await fetchWrapper.post({
                url: `${baseUrl}/user/login`,
                body: { "email": email, "password": password }
            });
            this.setToken(
                request?.user,
                { 'token': request?.token, 'date_expire': request?.expire_date_token },
                { 'token': request?.refresh_token, 'date_expire': request?.expire_date_refresh_token }
            );
            // redirect to previous url or default to home page if no previous url or if previous url is login page
            router.push((this.returnUrl && this.returnUrl !== '/login') ? this.returnUrl : '/');
        },
        async register(email, password, prenom, nom) {
            const request = fetchWrapper.post({
                url: `${baseUrl}/user`,
                body: { "email_user": email, "mdp_user": password, "nom_user": nom, "prenom_user": prenom, "role_user": "user" }
            });
            if (request) {
                this.login(email, password);
            } else {
                console.log('Error registering user', request);
            }
        },
        async refreshLogin() {
            const request = await fetchWrapper.post({
                url: `${baseUrl}/user/refresh-token`,
                useToken: "refresh"
            });
            this.setToken(
                request?.user,
                { 'token': request?.token, 'date_expire': request?.expire_date_token },
                { 'token': request?.refresh_token, 'date_expire': request?.expire_date_refresh_token }
            );
        },
        async forgotPassword(email) {
            return await fetchWrapper.post({
                url: `${baseUrl}/user/forgot-password`,
                body: { "email": email }
            });
        },
        async resetPassword(email, token, password) {
            const request = await fetchWrapper.post({
                url: `${baseUrl}/user/reset-password`,
                body: { "email": email, "token": token, "password": password }
            });
            if (request) {
                this.login(email, password);
            } else {
                console.log('Error resetting password', request);
            }
        },
        logout() {
            this.clearToken();
            router.push('/login');
        }
    }
});
