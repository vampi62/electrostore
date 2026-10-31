import { inject, onBeforeUnmount, watch } from "vue";
import { useI18n } from "vue-i18n";

import { isEditionDirty, snapshotEdition, applyDraft } from "@/helpers/editionDraft";
import type { RelatedStaged } from "@/helpers/editionDraft";
import { useTabsStore } from "@/stores";
import type { TabGuard } from "@/stores/tabs.store";
import type { useNotification } from "./useNotification";
import type { StoreGeneric } from "pinia";

// Watches the unsaved changes of the view displayed in a tab (new element, edition different from the saved element, changes staged in the *Ready of the store):
// the tab asks for a confirmation before being closed and the changes are kept in a draft when the application is closed.
// The draft of the tab is restored once the edition of the view is loaded.
// id: the id of the edited element (a new element has a "new-x" id), mainKey / sourceKey: keys of the store holding the edition and the saved elements,
// related: where the changes staged for the nested resources are when they have their own store.
export function useTabGuard(store: StoreGeneric, id: () => string, mainKey: string, sourceKey: string, related: RelatedStaged[] = []) {
	const tabId = inject<string | null>("tabId", null);
	if (!tabId) {
		return;
	}
	const tabsStore = useTabsStore();
	const { t } = useI18n();
	const notification = inject<ReturnType<typeof useNotification> | null>("useNotification", null);

	const guard: TabGuard = {
		isDirty: () => isEditionDirty(store, id(), mainKey, sourceKey, related),
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
