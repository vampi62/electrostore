import { reactive } from "vue";
import { getMessage } from "@/utils/messages";

interface Notification {
	id: number;
	message: string;
	type: string;
}

const notifications = reactive<Notification[]>([]);

export function useNotification() {
	// message can be a string or anything caught (Error, ApiError, API body): it is normalised to a string
	function addNotification({ message, type = "info" }: { message: unknown; type?: string }) {
		notifications.push({ id: Date.now(), message: getMessage(message, ""), type });
	}

	function removeNotification(id: number) {
		const index = notifications.findIndex((n) => n.id === id);
		if (index !== -1) {
			notifications.splice(index, 1);
		}
	}

	return { notifications, addNotification, removeNotification };
}
