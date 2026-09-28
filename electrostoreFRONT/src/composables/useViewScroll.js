import { inject } from "vue";

// tells the tab displaying the view if its scrollbar must always be visible
export function useViewScroll(alwaysVisible) {
	const viewScroll = inject("viewScroll", null);
	if (viewScroll) {
		viewScroll.value = alwaysVisible;
	}
}
