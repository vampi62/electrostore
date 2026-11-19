import { computed, nextTick, onBeforeUnmount, ref, watch } from "vue";
import type { Ref } from "vue";

interface PanelPosition {
	top: number;
	left: number;
	width: number;
}

// Panel (teleported in the body) opened under an anchor element.
// Handles its position, scroll / resize and click outside.
export function useFloatingPanel(anchor: Ref<HTMLElement | null>, panel: Ref<HTMLElement | null>) {
	const isOpen = ref(false);
	const position = ref<PanelPosition | null>(null);

	const style = computed<Record<string, string>>((): Record<string, string> => {
		if (!position.value) {
			return {};
		}
		return {
			top: `${position.value.top + 4}px`,
			left: `${position.value.left}px`,
			width: `${position.value.width}px`,
		};
	});

	function updatePosition() {
		if (!anchor.value) {
			return;
		}
		const rect = anchor.value.getBoundingClientRect();
		position.value = {
			top: rect.bottom + window.scrollY,
			left: rect.left + window.scrollX,
			width: rect.width,
		};
	}

	async function open() {
		isOpen.value = true;
		await nextTick();
		updatePosition();
	}

	function close() {
		isOpen.value = false;
	}

	async function toggle() {
		if (isOpen.value) {
			close();
		} else {
			await open();
		}
	}

	function onClickOutside(event: MouseEvent) {
		const target = event.target instanceof Node ? event.target : null;
		if (anchor.value?.contains(target) || panel.value?.contains(target)) {
			return;
		}
		close();
	}

	function addListeners() {
		window.addEventListener("scroll", updatePosition, true);
		window.addEventListener("resize", updatePosition);
		document.addEventListener("click", onClickOutside);
	}

	function removeListeners() {
		window.removeEventListener("scroll", updatePosition, true);
		window.removeEventListener("resize", updatePosition);
		document.removeEventListener("click", onClickOutside);
	}

	// listeners only exist while the panel is open
	watch(isOpen, (opened) => {
		if (opened) {
			addListeners();
		} else {
			removeListeners();
		}
	});
	onBeforeUnmount(removeListeners);

	return { isOpen, style, open, close, toggle, updatePosition };
}
