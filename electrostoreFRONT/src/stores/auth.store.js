import { defineStore } from 'pinia';

import { fetchWrapper, router } from '@/helpers';
import { useSessionTokenStore } from '@/stores';

const baseUrl = `${import.meta.env.VITE_API_URL}`;


export const useAuthStore = defineStore({
    id: 'auth',
    state: () => ({
        // initialize state from local storage to enable user to stay logged in
        user: JSON.parse(localStorage.getItem('user')),
        refeshToken: localStorage.getItem('refreshToken'),
        returnUrl: null,
        refreshInterval: null
    }),
    actions: {
        checkIfLoged() {
            const sessionTokenStore = useSessionTokenStore();
            if (!!this.user && !!this.refeshToken && !!sessionTokenStore.token) {
                this.startTokenRefreshCheck();
            }
        },
        async login(email, password) {
            const request = await fetchWrapper.post({
                url: `${baseUrl}/user/login`,
                body: { "email": email, "password": password }
            });
            // store user details and jwt in local storage to keep user logged in between page refreshes
            localStorage.setItem('user', JSON.stringify(request));
            localStorage.setItem('refreshToken', request?.refresh_token);
            this.user = request;
            this.refeshToken = request?.refresh_token;

            const sessionTokenStore = useSessionTokenStore();
            sessionTokenStore.setToken(request?.token);

            // redirect to previous url or default to home page if no previous url or if previous url is login page
            router.push((this.returnUrl && this.returnUrl !== '/login') ? this.returnUrl : '/');
            this.startTokenRefreshCheck();
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
        startTokenRefreshCheck() {
            // vérifier si le token est expiré
            const now = new Date().getTime();
            const timeToExpire = Date.parse(this.user.expire_date_token) - now;
            if (timeToExpire < 0) {
                this.refreshLogin();
            }
            // verifier si le token de rafraichissement est expiré
            const timeToExpireRefresh = Date.parse(this.user.expire_date_refresh_token) - now;
            if (timeToExpireRefresh < 0) {
                this.logout();
            }
            // vérifier toutes les 5 minutes si le token doit être rafraîchi
            this.refreshInterval = setInterval(() => {
                const now = new Date().getTime();
                const timeToExpire = Date.parse(this.user.expire_date_token) - now;
                const refreshThreshold = 10 * 60 * 1000; // 10 minutes avant l'expiration
                if (timeToExpire < refreshThreshold) {
                    this.refreshLogin();
                }
            }, 60 * 1000 * 5); // vérifier toutes les 5 minutes
        },
        stopTokenRefreshCheck() {
            clearInterval(this.refreshInterval);
        },
        async refreshLogin() {
            const request = await fetchWrapper.post({
                url: `${baseUrl}/user/refresh-token`,
                token: this.user.refesh_token
            });
            localStorage.setItem('user', JSON.stringify(request));
            localStorage.setItem('refreshToken', request?.refresh_token);
            this.user = request;
            this.refeshToken = request?.refresh_token;
            const sessionTokenStore = useSessionTokenStore();
            sessionTokenStore.setToken(request?.token);
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
            this.user = null;
            this.refeshToken = null;
            this.stopTokenRefreshCheck();
            localStorage.removeItem('user');

            const sessionTokenStore = useSessionTokenStore();
            sessionTokenStore.clearToken()
            localStorage.removeItem('refreshToken');
            router.push('/login');
        }
    }
});
