import { inject } from "vue";
import type { Ref } from "vue";

// tells the tab displaying the view if its scrollbar must always be visible
export function useViewScroll(alwaysVisible: boolean) {
	const viewScroll = inject<Ref<boolean> | null>("viewScroll", null);
	if (viewScroll) {
		viewScroll.value = alwaysVisible;
	}
}
