<script setup>
import { ref, provide, shallowReactive } from "vue";
import { RouterView, START_LOCATION, routerKey, routeLocationKey, routerViewLocationKey } from "vue-router";

import { useTabsStore } from "@/stores";

const props = defineProps({
	tabId: {
		type: String,
		required: true,
	},
	active: {
		type: Boolean,
		default: false,
	},
});

// every tab owns a router, the views displayed in the tab (useRouter, useRoute, RouterLink...) use it
// instead of the global one, this is what install() of vue-router does for the whole app
const router = useTabsStore().getRouter(props.tabId);
const reactiveRoute = {};
for (const key in START_LOCATION) {
	Object.defineProperty(reactiveRoute, key, {
		get: () => router.currentRoute.value[key],
		enumerable: true,
	});
}
provide(routerKey, router);
provide(routeLocationKey, shallowReactive(reactiveRoute));
provide(routerViewLocationKey, router.currentRoute);

// set by the views (useViewScroll) to always display the scrollbar
const viewScroll = ref(false);
provide("viewScroll", viewScroll);
</script>

<template>
	<div v-show="active"
		:class="['flex flex-col flex-1 min-h-0 px-4 pt-4', viewScroll ? 'overflow-y-scroll' : 'overflow-y-auto']">
		<RouterView />
	</div>
</template>
