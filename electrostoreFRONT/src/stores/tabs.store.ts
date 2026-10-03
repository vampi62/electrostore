import { defineStore } from "pinia";
import { createRouter, createMemoryHistory, type Router } from "vue-router";

import { appRoutes, defaultPath, blankPath, handleRouterError } from "@/router/routes";
import { createTabHistory } from "@/router/tabHistory";

export interface Tab {
	id: string;
	fullPath: string;
}

const storageKey = "tabs";

// one memory router per tab, kept outside of the (reactive) state
const tabRouters = new Map<string, Router>();

function createTabRouter(tabs: any, tabId: string, fullPath: string) {
	const router = createRouter({
		history: createTabHistory(),
		linkActiveClass: "active",
		routes: appRoutes,
	});
	router.onError(handleRouterError);
	router.beforeEach((to) => {
		if (to.path === "/" || !to.matched.length) {
			return defaultPath;
		}
	});
	router.afterEach((to) => {
		tabs.setPath(tabId, to.fullPath);
	});
	// the first navigation of a memory router replaces the initial location
	router.replace(fullPath);
	return router;
}

// used only to resolve titles and to normalize paths, it never navigates
const resolver = createRouter({ history: createMemoryHistory(), routes: appRoutes });

export function resolveTab(fullPath: string) {
	const resolved = resolver.resolve(fullPath);
	return {
		path: resolved.path,
		fullPath: resolved.matched.length && resolved.path !== "/" ? resolved.fullPath : defaultPath,
		params: resolved.params,
		meta: resolved.meta,
	};
}

function newTabId() {
	return Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
}

function loadSavedTabs() {
	try {
		const saved = JSON.parse(localStorage.getItem(storageKey));
		const tabs = saved.tabs.filter((tab) => typeof tab.id === "string" && typeof tab.fullPath === "string")
			.map((tab) => ({ id: tab.id, fullPath: resolveTab(tab.fullPath).fullPath }));
		if (tabs.length) {
			return { tabs, activeId: tabs.some((tab) => tab.id === saved.activeId) ? saved.activeId : tabs[0].id };
		}
	} catch {
		// no saved tabs or corrupted data
	}
	const tab = { id: newTabId(), fullPath: defaultPath };
	return { tabs: [tab], activeId: tab.id };
}

export const useTabsStore = defineStore("tabs", {
	state: () => ({
		...loadSavedTabs(),
		// when true (page embedded in an iframe) the tabs are not saved
		ephemeral: false,
	}),
	getters: {
		activeTab: (state) => state.tabs.find((tab) => tab.id === state.activeId),
	},
	actions: {
		save() {
			if (this.ephemeral) {
				return;
			}
			localStorage.setItem(storageKey, JSON.stringify({ tabs: this.tabs, activeId: this.activeId }));
		},
		getRouter(tabId: string) {
			if (!tabRouters.has(tabId)) {
				const tab = this.tabs.find((t) => t.id === tabId);
				tabRouters.set(tabId, createTabRouter(this, tabId, tab.fullPath));
			}
			return tabRouters.get(tabId);
		},
		setPath(tabId: string, fullPath: string) {
			const tab = this.tabs.find((t) => t.id === tabId);
			if (tab && tab.fullPath !== fullPath) {
				tab.fullPath = fullPath;
				this.save();
			}
		},
		// open a view in a tab, if it is already displayed in a tab this one is selected
		open(path: string = defaultPath) {
			const fullPath = resolveTab(path).fullPath;
			let tab = fullPath === blankPath ? undefined : this.tabs.find((t) => t.fullPath === fullPath);
			if (!tab) {
				tab = { id: newTabId(), fullPath };
				this.tabs.push(tab);
			}
			this.activate(tab.id);
		},
		// open an empty tab
		openBlank() {
			this.open(blankPath);
		},
		activate(tabId: string) {
			this.activeId = tabId;
			this.save();
		},
		// display a view in the selected tab
		navigate(path: string) {
			return this.getRouter(this.activeId).push(path);
		},
		close(tabId: string) {
			const index = this.tabs.findIndex((t) => t.id === tabId);
			if (index === -1) {
				return;
			}
			this.tabs.splice(index, 1);
			tabRouters.delete(tabId);
			if (this.tabs.length === 0) {
				this.open(defaultPath);
			} else if (this.activeId === tabId) {
				this.activate(this.tabs[Math.min(index, this.tabs.length - 1)].id);
			} else {
				this.save();
			}
		},
		// remove every tab and start again with a single one
		reset(path: string = defaultPath) {
			tabRouters.clear();
			this.tabs = [];
			this.open(path);
		},
	},
});
