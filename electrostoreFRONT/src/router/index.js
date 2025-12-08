import { createRouter, createWebHistory } from "vue-router";

import { useAuthStore } from "@/stores";

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	linkActiveClass: "active",
	routes: [
		{ path: "/", component: () => import("@/views/HomeView.vue") },
		{ path: "/profile", component: () => import("@/views/HomeView.vue") },
		{ path: "/login", component: () => import("@/views/LoginView.vue") },
		{ path: "/auth/callback", component: () => import("@/views/CallbackView.vue") },
		{ path: "/reset-password", component: () => import("@/views/ResetPasswordView.vue") },
		{ path: "/forgot-password", component: () => import("@/views/ForgotPasswordView.vue") },
		{ path: "/register", component: () => import("@/views/RegisterView.vue") },
		{ path: "/users", component: () => import("@/views/UsersView.vue") },
		{ path: "/users/:id", component: () => import("@/views/UserView.vue") },
		{ path: "/inventory", component: () => import("@/views/InventoryView.vue") },
		{ path: "/inventory/:id", component: () => import("@/views/ItemView.vue") },
		{ path: "/projets", component: () => import("@/views/ProjetsView.vue") },
		{ path: "/projets/:id", component: () => import("@/views/ProjetView.vue") },
		{ path: "/commands", component: () => import("@/views/CommandsView.vue") },
		{ path: "/commands/:id", component: () => import("@/views/CommandView.vue") },
		{ path: "/cameras", component: () => import("@/views/CamerasView.vue") },
		{ path: "/cameras/:id", component: () => import("@/views/CameraView.vue") },
		{ path: "/ia", component: () => import("@/views/IAsView.vue") },
		{ path: "/ia/:id", component: () => import("@/views/IAView.vue") },
		{ path: "/tags", component: () => import("@/views/TagsView.vue") },
		{ path: "/tags/:id", component: () => import("@/views/TagView.vue") },
		{ path: "/projet-tags", component: () => import("@/views/ProjetTagsView.vue") },
		{ path: "/projet-tags/:id", component: () => import("@/views/ProjetTagView.vue") },
		{ path: "/stores", component: () => import("@/views/StoresView.vue") },
		{ path: "/stores/:id", component: () => import("@/views/StoreView.vue") },
	],
});

router.beforeEach(async(to) => {
	const publicPages = ["/login", "/auth/callback", "/register", "/forgot-password", "/reset-password"];
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
		case "/":
			return "/inventory";
		}
	}
	// if page not exist redirect to home
	if (!to.matched.length) {
		return "/";
	}
});
export default router;