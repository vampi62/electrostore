import { reactive } from "vue";

const notifications = reactive([]);

export function useNotification() {
	function addNotification({ message, type = "info", i18n = false }) {
		notifications.push({ id: Date.now(), message, type, i18n });
	}

	function removeNotification(id) {
		const index = notifications.findIndex((n) => n.id === id);
		if (index !== -1) {
			notifications.splice(index, 1);
		}
	}

	return { notifications, addNotification, removeNotification };
}
