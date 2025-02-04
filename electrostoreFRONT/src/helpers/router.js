import { createRouter, createWebHistory } from "vue-router";

import { useAuthStore } from "@/stores";
import {
	HomeView,
	LoginView,
	ResetPasswordView,
	ForgotPasswordView,
	RegisterView,
	UsersView,
	UserView,
	InventoryView,
	ItemView,
	ProjetsView,
	ProjetView,
	CommandsView,
	CommandView,
	CamerasView,
	CameraView,
	IAsView,
	IAView,
	TagsView,
	TagView,
	StoresView,
	StoreView,
} from "@/views";

export const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	linkActiveClass: "active",
	routes: [
		{ path: "/", component: HomeView },
		{ path: "/profile", component: HomeView },
		{ path: "/login", component: LoginView },
		{ path: "/reset-password", component: ResetPasswordView },
		{ path: "/forgot-password", component: ForgotPasswordView },
		{ path: "/register", component: RegisterView },
		{ path: "/users", component: UsersView },
		{ path: "/users/:id", component: UserView },
		{ path: "/inventory", component: InventoryView },
		{ path: "/inventory/:id", component: ItemView },
		{ path: "/projets", component: ProjetsView },
		{ path: "/projets/:id", component: ProjetView },
		{ path: "/commands", component: CommandsView },
		{ path: "/commands/:id", component: CommandView },
		{ path: "/cameras", component: CamerasView },
		{ path: "/cameras/:id", component: CameraView },
		{ path: "/ia", component: IAsView },
		{ path: "/ia/:id", component: IAView },
		{ path: "/tags", component: TagsView },
		{ path: "/tags/:id", component: TagView },
		{ path: "/stores", component: StoresView },
		{ path: "/stores/:id", component: StoreView },
	],
});

router.beforeEach(async(to) => {
	const publicPages = ["/login", "/register", "/forgot-password", "/reset-password"];
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
});
