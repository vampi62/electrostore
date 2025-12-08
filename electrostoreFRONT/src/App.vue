<script setup>
import { ref, computed } from "vue";

import { RouterView, useRoute } from "vue-router";
const route = useRoute();

import { useAuthStore, useConfigsStore } from "@/stores";

const configsStore = useConfigsStore();
const authStore = useAuthStore();

configsStore.getConfig();

const isIframe = computed(() => typeof route.query.iframe !== "undefined");

const reduceLeftSideBar = ref(false);
const listNav = [
	{ name: "common.VAppInventory", path: "/inventory", roleRequired: "user", faIcon: "fa-solid fa-box" },
	{ name: "common.VAppProjet", path: "/projets", roleRequired: "user", faIcon: "fa-solid fa-project-diagram" },
	{ name: "common.VAppCommand", path: "/commands", roleRequired: "user", faIcon: "fa-solid fa-shopping-cart" },
	{ name: "common.VAppCam", path: "/cameras", roleRequired: "user", faIcon: "fa-solid fa-camera" },
	{ name: "common.VAppIa", path: "/ia", roleRequired: "user", faIcon: "fa-solid fa-microchip" },
	{ name: "common.VAppTags", path: "/tags", roleRequired: "user", faIcon: "fa-solid fa-tags" },
	{ name: "common.VAppStores", path: "/stores", roleRequired: "user", faIcon: "fa-solid fa-store" },
];

const containerClasses = computed(() => [
	"px-4 pt-4 overflow-y-scroll fixed bottom-0 right-0 left-0 flex flex-col",
	reduceLeftSideBar.value && authStore.user && !isIframe.value ? "sm:ml-16" : "",
	!reduceLeftSideBar.value && authStore.user && !isIframe.value ? "sm:ml-64" : "",
	authStore.user && !isIframe.value ? "top-16" : "top-0",
]);
const modalFinderRef = ref(null);

const showAboutModal = ref(false);
</script>

<template>
	<div v-show="authStore.user && !isIframe">
		<NavBar :list-nav="listNav" :load-page-find="modalFinderRef?.loadPageFind || (() => {})"
			@update:reduce-left-side-bar="reduceLeftSideBar = $event" @show-about-modal="showAboutModal = true" />
	</div>
	<div :class="containerClasses">
		<RouterView />
	</div>
	<ModalFinder ref="modalFinderRef" />
	<NotificationContainer />
	<div v-if="showAboutModal" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-20" @click="showAboutModal = false">
		<div class="bg-white rounded-lg shadow-lg w-3/4 h-3/4 p-6" @click.stop>
		</div>
	</div>

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