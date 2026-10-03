<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";

import { useTabsStore, resolveTab } from "@/stores";
import type { Tab } from "@/stores/tabs.store";

const { t } = useI18n();
const tabsStore = useTabsStore();

const showTabList = ref(false);

function tabInfo(tab: Tab) {
	const resolved = resolveTab(tab.fullPath);
	let title = resolved.meta.title ? t(resolved.meta.title as string) : resolved.path;
	const id = resolved.params.id;
	if (id !== undefined) {
		title += " · " + (id === "new" ? t("common.VAppTabNew") : "#" + id);
	}
	return { title, icon: resolved.meta.icon };
}

function selectTab(tabId: string) {
	tabsStore.activate(tabId);
	showTabList.value = false;
}
</script>

<template>
	<div class="flex items-center flex-1 min-w-0 gap-1">
		<div class="flex items-center gap-1 min-w-0 overflow-x-auto no-scrollbar" role="tablist">
			<div v-for="tab in tabsStore.tabs" :key="tab.id" role="tab" tabindex="0"
				:aria-selected="tab.id === tabsStore.activeId" :title="tabInfo(tab).title"
				:class="['flex items-center gap-2 h-9 pl-3 pr-2 rounded flex-shrink-0 max-w-[12rem] text-sm cursor-pointer select-none',
					tab.id === tabsStore.activeId ? 'bg-gray-600 text-white border-b-2 border-blue-400' : 'text-gray-300 hover:bg-gray-700']"
				@click="tabsStore.activate(tab.id)" @keydown.enter="tabsStore.activate(tab.id)"
				@mousedown.middle.prevent @auxclick.middle.prevent="tabsStore.close(tab.id)">
				<font-awesome-icon v-if="tabInfo(tab).icon" :icon="tabInfo(tab).icon" class="flex-shrink-0" />
				<span class="truncate">{{ tabInfo(tab).title }}</span>
				<button v-if="tabsStore.tabs.length > 1" type="button" :aria-label="$t('common.VAppTabClose')"
					:title="$t('common.VAppTabClose')"
					class="flex items-center justify-center w-5 h-5 rounded flex-shrink-0 hover:bg-gray-500"
					@click.stop="tabsStore.close(tab.id)">
					<font-awesome-icon icon="fa-solid fa-xmark" size="xs" />
				</button>
			</div>
		</div>
		<div class="relative flex-shrink-0">
			<!-- lists every open tab, this is the only way to reach a tab once there are too many to fit (scrolling the strip works too, but this is the reliable path, mobile included) -->
			<button type="button" :aria-label="$t('common.VAppTabList')" :title="$t('common.VAppTabList')"
				class="flex items-center justify-center w-9 h-9 rounded flex-shrink-0 text-white hover:bg-gray-700 hover:text-blue-400"
				@click="showTabList = !showTabList">
				<font-awesome-icon icon="fa-solid fa-chevron-down" />
			</button>
			<template v-if="showTabList">
				<div class="fixed inset-0" @click="showTabList = false"></div>
				<ul class="absolute right-0 top-full mt-2 w-64 max-h-96 overflow-y-auto py-1 bg-gray-800 border border-blue-400 rounded shadow-lg z-20">
					<li v-for="tab in tabsStore.tabs" :key="tab.id" class="flex items-center">
						<button type="button" :title="tabInfo(tab).title"
							:class="['flex items-center gap-3 flex-1 min-w-0 px-4 py-2 text-left text-sm hover:bg-gray-700 hover:text-blue-400',
								tab.id === tabsStore.activeId ? 'text-blue-400' : 'text-white']"
							@click="selectTab(tab.id)">
							<font-awesome-icon v-if="tabInfo(tab).icon" :icon="tabInfo(tab).icon" class="flex-shrink-0" />
							<span class="truncate">{{ tabInfo(tab).title }}</span>
						</button>
						<button v-if="tabsStore.tabs.length > 1" type="button" :aria-label="$t('common.VAppTabClose')"
							:title="$t('common.VAppTabClose')"
							class="flex items-center justify-center w-8 h-8 mr-1 rounded flex-shrink-0 text-white hover:bg-gray-600"
							@click="tabsStore.close(tab.id)">
							<font-awesome-icon icon="fa-solid fa-xmark" size="xs" />
						</button>
					</li>
				</ul>
			</template>
		</div>
		<button type="button" :aria-label="$t('common.VAppTabAdd')" :title="$t('common.VAppTabAdd')"
			class="flex items-center justify-center w-9 h-9 rounded flex-shrink-0 text-white hover:bg-gray-700 hover:text-blue-400"
			@click="tabsStore.openBlank()">
			<font-awesome-icon icon="fa-solid fa-plus" />
		</button>
	</div>
</template>
