import { reactive } from 'vue';

const notifications = reactive([]);

export function useNotification() {
    function addNotification(message, type = 'info') {
        notifications.push({ id: Date.now(), message, type });
    }

    function removeNotification(id) {
        const index = notifications.findIndex((n) => n.id === id);
        if (index !== -1) notifications.splice(index, 1);
    }

    return { notifications, addNotification, removeNotification };
}
