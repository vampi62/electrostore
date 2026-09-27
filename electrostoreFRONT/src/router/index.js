import { createRouter, createWebHistory } from "vue-router";

import { useAuthStore } from "@/stores";
import { publicPages, handleRouterError } from "./routes";

// The global router only handles the public pages (login, register...) and the browser url.
// Every authenticated page is displayed inside a tab, each tab owns its own router (see tabs.store.js),
// so every path that is not a public page matches the "app" route.
const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	linkActiveClass: "active",
	routes: [
		{ path: "/auth/callback", component: () => import("@/views/CallbackView.vue") },
		{ path: "/forgot-password", component: () => import("@/views/ForgotPasswordView.vue") },
		{ path: "/login", component: () => import("@/views/LoginView.vue") },
		{ path: "/reset-password", component: () => import("@/views/ResetPasswordView.vue") },
		{ path: "/register", component: () => import("@/views/RegisterView.vue") },
		{ path: "/:pathMatch(.*)*", component: { render: () => null }, meta: { app: true } },
	],
});

router.onError(handleRouterError);

router.beforeEach(async(to) => {
	const authRequired = !publicPages.includes(to.path);
	const auth = useAuthStore();

	if (authRequired && !auth.user) {
		auth.returnUrl = to.fullPath;
		return "/login";
	}
	if (auth.user) {
		switch (to.path) {
		case "/login":
			return "/";
		case "/register":
			return "/";
		case "/forgot-password":
			return "/";
		case "/reset-password":
			return "/";
		}
	}
});
export default router;
