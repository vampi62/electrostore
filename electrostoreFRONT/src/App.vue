<script setup lang="ts">
import { ref, computed, watch } from "vue";

import { RouterView, useRoute, useRouter } from "vue-router";
const route = useRoute();
const router = useRouter();

import { useAuthStore, useConfigsStore, useTabsStore } from "@/stores";

const configsStore = useConfigsStore();
const authStore = useAuthStore();
const tabsStore = useTabsStore();

configsStore.getConfig();
configsStore.getHealth();

const isIframe = computed(() => route.query.iframe !== undefined);
// authenticated pages are displayed in tabs, the global router only handles the public pages
const isTabsView = computed(() => route.meta.app === true);

// the browser url follows the selected tab
watch((): [boolean, string | undefined] => [isTabsView.value, tabsStore.activeTab?.fullPath], ([tabsView, fullPath]) => {
	if (tabsView && fullPath && route.fullPath !== fullPath) {
		router.replace(fullPath);
	}
});
// an url opened from outside (bookmark, link, login...) is displayed in a tab
watch((): [boolean, string] => [isTabsView.value, route.fullPath], ([tabsView, fullPath]) => {
	if (!tabsView) {
		return;
	}
	if (route.query.iframe !== undefined && !tabsStore.ephemeral) {
		tabsStore.ephemeral = true;
		tabsStore.reset(fullPath);
	} else if (fullPath === "/") {
		router.replace(tabsStore.activeTab.fullPath);
	} else if (fullPath !== tabsStore.activeTab?.fullPath) {
		tabsStore.open(fullPath);
	}
}, { immediate: true });
// the unsaved changes of the tabs are kept in a draft when the page is closed (or hidden, the only reliable event on mobile)
window.addEventListener("pagehide", () => tabsStore.saveDrafts());
document.addEventListener("visibilitychange", () => {
	if (document.visibilityState === "hidden") {
		tabsStore.saveDrafts();
	}
});
// tabs are not kept from a user to another
watch(() => authStore.user, (user) => {
	if (!user) {
		tabsStore.reset();
	}
});

const reduceLeftSideBar = ref(false);
const listNav = ref([
	{ name: "common.VAppInventory", path: "/inventory", faIcon: "fa-solid fa-box" },
	{ name: "common.VAppProject", path: "/projects", faIcon: "fa-solid fa-project-diagram" },
	{ name: "common.VAppCommand", path: "/commands", faIcon: "fa-solid fa-shopping-cart" },
	{ name: "common.VAppTags", path: "/tags", faIcon: "fa-solid fa-tags" },
	{ name: "common.VAppStores", path: "/stores", faIcon: "fa-solid fa-store" },
	{ name: "common.VAppZones", path: "/zones", faIcon: "fa-solid fa-map" },
	{ name: "common.VAppEquipements", path: "/equipements", faIcon: "fa-solid fa-screwdriver-wrench" },
	{ name: "common.VAppCronJobs", path: "/cronjobs", faIcon: "fa-solid fa-clock" },
]);

const containerClasses = computed(() => [
	"fixed bottom-0 right-0 left-0 flex flex-col",
	isTabsView.value ? "overflow-hidden" : "px-4 pt-4",
	reduceLeftSideBar.value && authStore.user && !isIframe.value ? "sm:ml-16" : "",
	!reduceLeftSideBar.value && authStore.user && !isIframe.value ? "sm:ml-64" : "",
	authStore.user && !isIframe.value ? "top-16" : "top-0",
	!isTabsView.value && !route.meta.overflowYScroll ? "overflow-y-auto" : "",
]);

const showAboutModal = ref(false);
const showAiModal = ref(false);
</script>

<template>
	<div v-show="authStore.user && !isIframe">
		<NavBar :list-nav="listNav"
			@update:reduce-left-side-bar="reduceLeftSideBar = $event" @show-about-modal="showAboutModal = true" @show-ai-modal="showAiModal = true" />
	</div>
	<div id="view" :class="containerClasses">
		<TabViews v-if="isTabsView" />
		<RouterView v-else />
	</div>
	<AiChatModal v-if="authStore.user" :show-modal="showAiModal" @close-modal="showAiModal = false" />
	<NotificationContainer />
	<NotificationAppUpdate />
	<AboutModal v-if="authStore.user" :show-modal="showAboutModal" @close-modal="showAboutModal = false" />
</template>

<style>
.no-scrollbar::-webkit-scrollbar {
	display: none;
}

.no-scrollbar {
	-ms-overflow-style: none;
	/* IE and Edge */
	scrollbar-width: none;
	/* Firefox */
}
</style>