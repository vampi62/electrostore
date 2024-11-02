import { defineStore } from 'pinia';

import { fetchWrapper, router } from '@/helpers';

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useAuthStore = defineStore({
    id: 'auth',
    state: () => ({
        // initialize state from local storage to enable user to stay logged in
        token: null,
        expireDate: null,
        refreshToken: null,
        expireRefreshDate: null,
        user: JSON.parse(localStorage.getItem('user')),
        returnUrl: null
    }),
    actions: {
        async login(email, password) {
            const user = await fetchWrapper.post(`${baseUrl}/user/login`, {"email": email, "password": password});
            this.token = user.token;
            this.expireDate = user.expire_date;
            this.refreshToken = user.refesh_token;
            this.expireRefreshDate = user.expire_refresh_token
            // store user details and jwt in local storage to keep user logged in between page refreshes
            localStorage.setItem('user', JSON.stringify(user));
            // redirect to previous url or default to home page
            router.push(this.returnUrl || '/');
        },
        async register(email, password, prenom, nom) {
            const user = fetchWrapper.post(`${baseUrl}/user`, {"email_user": email, "mdp_user": password, "nom_user": nom, "prenom_user": prenom, "role_user": "user"});
            if (user) {
                this.login(email, password);
            } else {
                console.log('Error registering user', user);
            }
        },
        async forgotPassword(email) {
            return await fetchWrapper.post(`${baseUrl}/user/forgot-password`, {"email": email});
        },
        async resetPassword(email, token, password) {
            await fetchWrapper.post(`${baseUrl}/user/reset-password`, {"email": email, "token": token, "password": password});
        },
        logout() {
            this.user = null;
            this.token = null;
            this.expireDate = null;
            this.refreshToken = null;
            this.expireRefreshDate = null;
            localStorage.removeItem('user');
            router.push('/login');
        }
    }
});
