// pages accessible without authentication (handled by the global router)
export const publicPages = ["/login", "/auth/callback", "/register", "/forgot-password", "/reset-password"];

// default view opened in a new tab and target of "/"
export const defaultPath = "/inventory";

// empty page of a new tab, the user chooses the view to display with the menu
export const blankPath = "/blank";

// pages displayed inside tabs (each tab owns a memory router using these routes)
// meta.title: i18n key of the tab title, meta.icon: font awesome icon of the tab
export const appRoutes = [
	{ path: blankPath, component: () => import("@/views/BlankView.vue"), meta: { title: "common.VAppTabAdd", icon: "fa-solid fa-plus" } },
	{ path: "/", component: () => import("@/views/HomeView.vue"), meta: { title: "common.VAppHome", icon: "fa-solid fa-box" } },
	{ path: "/cronjobs", component: () => import("@/views/CronJobsView.vue"), meta: { title: "common.VAppCronJobs", icon: "fa-solid fa-clock" } },
	{ path: "/cronjobs/:id", component: () => import("@/views/CronJobView.vue"), meta: { title: "common.VAppCronJobs", icon: "fa-solid fa-clock" } },
	{ path: "/equipements", component: () => import("@/views/EquipementsView.vue"), meta: { title: "common.VAppEquipements", icon: "fa-solid fa-screwdriver-wrench" } },
	{ path: "/equipements/:id", component: () => import("@/views/EquipementView.vue"), meta: { title: "common.VAppEquipements", icon: "fa-solid fa-screwdriver-wrench" } },
	{ path: "/commands", component: () => import("@/views/CommandsView.vue"), meta: { title: "common.VAppCommand", icon: "fa-solid fa-shopping-cart" } },
	{ path: "/commands/:id", component: () => import("@/views/CommandView.vue"), meta: { title: "common.VAppCommand", icon: "fa-solid fa-shopping-cart" } },
	{ path: "/health", component: () => import("@/views/HealthView.vue"), meta: { title: "common.VAppHealth", icon: "fa-solid fa-box" } },
	{ path: "/inventory", component: () => import("@/views/InventoryView.vue"), meta: { title: "common.VAppInventory", icon: "fa-solid fa-box" } },
	{ path: "/inventory/:id", component: () => import("@/views/ItemView.vue"), meta: { title: "common.VAppInventory", icon: "fa-solid fa-box" } },
	{ path: "/profile", component: () => import("@/views/HomeView.vue"), meta: { title: "common.VAppProfile", icon: "fa-solid fa-box" } },
	{ path: "/project-tags", component: () => import("@/views/ProjectTagsView.vue"), meta: { title: "common.VAppProject", icon: "fa-solid fa-project-diagram" } },
	{ path: "/project-tags/:id", component: () => import("@/views/ProjectTagView.vue"), meta: { title: "common.VAppProject", icon: "fa-solid fa-project-diagram" } },
	{ path: "/projects", component: () => import("@/views/ProjectsView.vue"), meta: { title: "common.VAppProject", icon: "fa-solid fa-project-diagram" } },
	{ path: "/projects/:id", component: () => import("@/views/ProjectView.vue"), meta: { title: "common.VAppProject", icon: "fa-solid fa-project-diagram" } },
	{ path: "/stores", component: () => import("@/views/StoresView.vue"), meta: { title: "common.VAppStores", icon: "fa-solid fa-store" } },
	{ path: "/stores/:id", component: () => import("@/views/StoreView.vue"), meta: { title: "common.VAppStores", icon: "fa-solid fa-store" } },
	{ path: "/tags", component: () => import("@/views/TagsView.vue"), meta: { title: "common.VAppTags", icon: "fa-solid fa-tags" } },
	{ path: "/tags/:id", component: () => import("@/views/TagView.vue"), meta: { title: "common.VAppTags", icon: "fa-solid fa-tags" } },
	{ path: "/users", component: () => import("@/views/UsersView.vue"), meta: { title: "common.VAppAdmin", icon: "fa-solid fa-box" } },
	{ path: "/users/:id", component: () => import("@/views/UserView.vue"), meta: { title: "common.VAppProfile", icon: "fa-solid fa-box" } },
	{ path: "/zones", component: () => import("@/views/ZonesView.vue"), meta: { title: "common.VAppZones", icon: "fa-solid fa-map" } },
	{ path: "/zones/:id", component: () => import("@/views/ZoneView.vue"), meta: { title: "common.VAppZones", icon: "fa-solid fa-map" } },
];

export function handleRouterError(error, to) {
	const isChunkError =
		error.message.includes("Failed to fetch dynamically imported module") ||
		error.message.includes("Importing a module script failed") ||
		error.message.includes("Unable to preload CSS");

	if (isChunkError) {
		console.warn("Chunk load error detected, attempting to reload the page:", error);
		// Avoid infinite loop
		const lastReload = sessionStorage.getItem("lastChunkReload");
		const now = Date.now();
		if (!lastReload || (now - Number.parseInt(lastReload)) > 10000) {
			sessionStorage.setItem("lastChunkReload", now.toString());
			window.location.href = to.fullPath;
		} else {
			console.error("Persistent error after reload, check your connection or contact support.");
		}
	}
}
