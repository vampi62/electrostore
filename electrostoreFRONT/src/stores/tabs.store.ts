import { defineStore } from "pinia";
import { createRouter, createMemoryHistory, type Router } from "vue-router";

import { appRoutes, defaultPath, blankPath, handleRouterError } from "@/router/routes";
import { createTabHistory } from "@/router/tabHistory";
import type { EditionDraft } from "@/helpers/editionDraft";

export interface Tab {
	id: string;
	fullPath: string;
}

const storageKey = "tabs";
const draftsKey = "tabDrafts";

// one memory router per tab, kept outside of the (reactive) state
const tabRouters = new Map<string, Router>();

// the view displayed in a tab tells if it holds unsaved changes (see useTabGuard)
export interface TabGuard {
	isDirty: () => boolean;
	snapshot: () => EditionDraft;
}
const tabGuards = new Map<string, TabGuard>();

export type LeaveChoice = "leave" | "newTab" | "cancel";
let answerLeave: ((choice: LeaveChoice) => void) | null = null;

export interface TabDraft {
	fullPath: string;
	savedAt: number;
	draft: EditionDraft;
}

function readDrafts(): Record<string, TabDraft> {
	try {
		return JSON.parse(localStorage.getItem(draftsKey)) ?? {};
	} catch {
		return {};
	}
}

function writeDrafts(drafts: Record<string, TabDraft>) {
	try {
		if (Object.keys(drafts).length) {
			localStorage.setItem(draftsKey, JSON.stringify(drafts));
		} else {
			localStorage.removeItem(draftsKey);
		}
	} catch {
		// storage full or unavailable: the draft is lost, nothing else can be done
	}
}

function createTabRouter(tabs: any, tabId: string, fullPath: string) {
	const router = createRouter({
		history: createTabHistory(),
		linkActiveClass: "active",
		routes: appRoutes,
	});
	router.onError(handleRouterError);
	router.beforeEach(async(to, from) => {
		if (to.path === "/" || !to.matched.length) {
			return defaultPath;
		}
		// leaving a view that holds unsaved changes: stay, leave anyway or open the destination in a new tab
		if (to.path !== from.path && tabs.isDirty(tabId)) {
			const choice = await tabs.askLeave(tabId, to.fullPath);
			if (choice === "newTab") {
				tabs.open(to.fullPath);
			}
			if (choice !== "leave") {
				return false;
			}
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
		// tabs displaying a view that watches its unsaved changes (the guards themselves are not reactive)
		guarded: {} as Record<string, boolean>,
		// navigation waiting for the user's answer (see askLeave)
		pendingLeave: null as { tabId: string; path: string } | null,
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
		registerGuard(tabId: string, guard: TabGuard) {
			tabGuards.set(tabId, guard);
			this.guarded[tabId] = true;
		},
		// the view is gone (closed tab or other view), its edition is cleared so is its draft
		unregisterGuard(tabId: string, guard: TabGuard) {
			if (tabGuards.get(tabId) !== guard) {
				return;
			}
			tabGuards.delete(tabId);
			delete this.guarded[tabId];
			this.deleteDraft(tabId);
		},
		askLeave(tabId: string, path: string) {
			answerLeave?.("cancel");
			return new Promise<LeaveChoice>((resolve) => {
				this.pendingLeave = { tabId, path };
				answerLeave = (choice) => {
					answerLeave = null;
					this.pendingLeave = null;
					resolve(choice);
				};
			});
		},
		answerLeave(choice: LeaveChoice) {
			answerLeave?.(choice);
		},
		isDirty(tabId: string) {
			return !!this.guarded[tabId] && !!tabGuards.get(tabId)?.isDirty();
		},
		// keep the unsaved changes of every tab to restore them when the application is opened again
		saveDrafts() {
			if (this.ephemeral) {
				return;
			}
			const drafts = readDrafts();
			for (const tab of this.tabs) {
				const guard = tabGuards.get(tab.id);
				if (guard?.isDirty()) {
					drafts[tab.id] = { fullPath: tab.fullPath, savedAt: Date.now(), draft: guard.snapshot() };
				} else if (guard) {
					delete drafts[tab.id];
				}
			}
			for (const tabId of Object.keys(drafts)) {
				if (!this.tabs.some((t) => t.id === tabId)) {
					delete drafts[tabId];
				}
			}
			writeDrafts(drafts);
		},
		getDraft(tabId: string): TabDraft | null {
			const draft = readDrafts()[tabId];
			const tab = this.tabs.find((t) => t.id === tabId);
			return draft && draft.fullPath === tab?.fullPath ? draft : null;
		},
		deleteDraft(tabId: string) {
			if (this.ephemeral) {
				return;
			}
			const drafts = readDrafts();
			if (drafts[tabId]) {
				delete drafts[tabId];
				writeDrafts(drafts);
			}
		},
		close(tabId: string) {
			const index = this.tabs.findIndex((t) => t.id === tabId);
			if (index === -1) {
				return;
			}
			this.tabs.splice(index, 1);
			tabRouters.delete(tabId);
			this.deleteDraft(tabId);
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
			if (!this.ephemeral) {
				writeDrafts({});
			}
			this.tabs = [];
			this.open(path);
		},
	},
});
