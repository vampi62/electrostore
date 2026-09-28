<script setup>
import { reactive, watch } from "vue";

import { useTabsStore } from "@/stores";

const tabsStore = useTabsStore();

// the views of a tab are loaded the first time the tab is selected then kept alive
const loadedTabs = reactive(new Set());
watch(() => tabsStore.activeId, (id) => loadedTabs.add(id), { immediate: true });
</script>

<template>
	<template v-for="tab in tabsStore.tabs" :key="tab.id">
		<TabHost v-if="loadedTabs.has(tab.id)" :tab-id="tab.id" :active="tab.id === tabsStore.activeId" />
	</template>
</template>
