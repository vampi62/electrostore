import { defineStore } from "pinia";

import { fetchWrapper } from "@/helpers";
import router from "@/router";

const baseUrl = `${import.meta.env.VITE_API_URL}`;

export const useAuthStore = defineStore("auth",{
	state: () => ({
		// initialize state from local storage to enable user to stay logged in
		user: JSON.parse(localStorage.getItem("user")) || null,
		accessToken: JSON.parse(localStorage.getItem("accessToken")) || null,
		refreshToken: JSON.parse(localStorage.getItem("refreshToken")) || null,
		isSSOUser: JSON.parse(localStorage.getItem("isSSOUser")) || false,
		selectedProvider: JSON.parse(localStorage.getItem("selectedProvider")) || null,
	}),
	actions: {
		hasPermission(requiredPermission) {
			if (!this.user?.role_user) {
				return false;
			}
			const userPermissions = this.user.role_user || [];
			// check if the permission user is a list or a single string
			if (Array.isArray(userPermissions)) {
				// check if the user has at least one of the required permissions
				return requiredPermission.some((permission) => userPermissions.includes(permission));
			} else {
				// if the user permissions is a single string, check if it matches any of the required permissions
				return requiredPermission.includes(userPermissions);
			}
		},
		setToken(user, accessToken, refreshToken, isSSOUser = false) {
			this.accessToken = accessToken;
			this.refreshToken = refreshToken;
			this.user = user;
			this.user.isSSOUser = isSSOUser;
			localStorage.setItem("accessToken", JSON.stringify(accessToken));
			localStorage.setItem("refreshToken", JSON.stringify(refreshToken));
			localStorage.setItem("user", JSON.stringify(user));
		},
		clearToken() {
			this.accessToken = null;
			this.refreshToken = null;
			this.user = null;
			localStorage.removeItem("accessToken");
			localStorage.removeItem("refreshToken");
			localStorage.removeItem("user");
		},
		TokenIsExpired() {
			// if date expire is less than current date + 5 minutes
			if (new Date(this.accessToken.date_expire).getTime() < new Date().getTime() + 5 * 60000) {
				console.log("Token is expired");
				return true;
			}
			return false;
		},
		RefreshTokenIsExpired() {
			// if date expire is less than current date
			if (new Date(this.refreshToken.date_expire).getTime() < new Date().getTime()) {
				console.log("Refresh Token is expired");
				return true;
			}
			return false;
		},
		async login(email, password) {
			const request = await fetchWrapper.post({
				url: `${baseUrl}/auth/login`,
				body: { "email": email, "password": password },
			});
			this.setToken(
				request?.user,
				{ "token": request?.token, "date_expire": request?.expire_date_token },
				{ "token": request?.refresh_token, "date_expire": request?.expire_date_refresh_token },
				false,
			);
			router.push("/");
		},
		async loginSSO(provider) {
			const request = await fetchWrapper.get({
				url: `${baseUrl}/auth/${provider}/url`,
			});
			this.selectedProvider = provider;
			localStorage.setItem("selectedProvider", JSON.stringify(provider));
			// open small window to the url
			window.open(request.authUrl, "SSO Login", "width=600,height=600");
		},
		async handleSSOCallback() {
			const params = new URLSearchParams(window.location.search);
			const token = params.get("code");
			const state = params.get("state");
			if (token && state) {
				const request = await fetchWrapper.post({
					url: `${baseUrl}/auth/${this.selectedProvider}/callback`,
					body: { "Code": token, "State": state },
				});
				this.setToken(
					request?.user,
					{ "token": request?.token, "date_expire": request?.expire_date_token },
					{ "token": request?.refresh_token, "date_expire": request?.expire_date_refresh_token },
					true,
				);
				localStorage.removeItem("selectedProvider");
				// close small window and refresh parent window
				window.opener.location.reload();
				window.close();
			}
		},
		async register(email, password, prenom, nom) {
			const request = fetchWrapper.post({
				url: `${baseUrl}/user`,
				body: { "email_user": email, "mdp_user": password, "nom_user": nom, "prenom_user": prenom, "role_user": "user" },
			});
			if (request) {
				this.login(email, password);
			} else {
				console.log("Error registering user", request);
			}
		},
		async refreshLogin() {
			const request = await fetchWrapper.post({
				url: `${baseUrl}/auth/refresh-token`,
				useToken: "refresh",
			});
			this.setToken(
				request?.user,
				{ "token": request?.token, "date_expire": request?.expire_date_token },
				{ "token": request?.refresh_token, "date_expire": request?.expire_date_refresh_token },
				this.isSSOUser,
			);
		},
		async forgotPassword(email) {
			return await fetchWrapper.post({
				url: `${baseUrl}/auth/forgot-password`,
				body: { "email": email },
			});
		},
		async resetPassword(email, token, password) {
			const request = await fetchWrapper.post({
				url: `${baseUrl}/auth/reset-password`,
				body: { "email": email, "token": token, "password": password },
			});
			if (request) {
				this.login(email, password);
			} else {
				console.log("Error resetting password", request);
			}
		},
		logout() {
			this.clearToken();
			router.push("/login");
		},
	},
});
