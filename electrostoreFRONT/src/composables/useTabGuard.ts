import { inject, onBeforeUnmount, shallowReactive, watch } from "vue";
import { useI18n } from "vue-i18n";

import { fingerprintEdition, snapshotEdition, applyDraft } from "@/helpers/editionDraft";
import type { RelatedStaged } from "@/helpers/editionDraft";
import { useTabsStore } from "@/stores";
import type { TabGuard } from "@/stores/tabs.store";
import type { useNotification } from "./useNotification";
import type { StoreGeneric } from "pinia";

// Watches the unsaved changes of the view displayed in a tab (edition or changes staged in the *Ready of the store that differ from the baseline):
// the tab asks for a confirmation before being closed and the changes are kept in a draft when the application is closed.
// The baseline of an element is its edition right after store.loadToEdition (default values and preset included), so what the store fills in
// by itself is not a change of the user. An element without baseline (not loaded yet) is never dirty.
// The draft of the tab is restored once the edition of the view is loaded.
// id: the id of the edited element (a new element has a "new-x" id), mainKey: key of the store holding the edition,
// related: where the changes staged for the nested resources are when they have their own store.
export function useTabGuard(store: StoreGeneric, id: () => string, mainKey: string, related: RelatedStaged[] = []) {
	const tabId = inject<string | null>("tabId", null);
	if (!tabId) {
		return;
	}
	const tabsStore = useTabsStore();
	const { t } = useI18n();
	const notification = inject<ReturnType<typeof useNotification> | null>("useNotification", null);

	// reactive so the unsaved mark of the tab is updated when a baseline is taken
	const baselines = shallowReactive<Record<string, string>>({});
	store.$onAction(({ name, args, after }) => {
		if (name !== "loadToEdition") {
			return;
		}
		const loadedId = String(args[0]);
		after(() => {
			baselines[loadedId] = fingerprintEdition(store, loadedId, mainKey, related);
		});
	});

	const guard: TabGuard = {
		isDirty: () => {
			const baseline = baselines[id()];
			return baseline !== undefined && fingerprintEdition(store, id(), mainKey, related) !== baseline;
		},
		snapshot: () => snapshotEdition(store, id(), mainKey, related),
	};
	tabsStore.registerGuard(tabId, guard);
	onBeforeUnmount(() => tabsStore.unregisterGuard(tabId, guard));

	const saved = tabsStore.getDraft(tabId);
	if (!saved) {
		return;
	}
	let restored = false;
	watch(() => store.$state[mainKey]?.[id()]?.loading, (loading) => {
		if (restored || loading !== false) {
			return;
		}
		restored = true;
		applyDraft(store, id(), mainKey, saved.draft, related);
		notification?.addNotification({ message: t("common.VAppTabDraftRestored"), type: "info" });
		if (saved.draft.lost) {
			notification?.addNotification({ message: t("common.VAppTabDraftLostFiles", { count: saved.draft.lost }), type: "error" });
		}
	}, { immediate: true });
}
