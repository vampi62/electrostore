<template>
	<div class="notification-container">
		<Notification v-for="(notification) in notifications" :key="notification.id" :id="notification.id"
			:message="notification.message" :type="notification.type" :i18n="notification.i18n"
			@remove="removeNotification" />
	</div>
</template>

<script>
import { defineAsyncComponent } from "vue";
import { inject } from "vue";

export default {
	name: "NotificationContainer",
	components: {
		Notification: defineAsyncComponent(() => import("@/components/Notification.vue")),
	},
	setup() {
		const { notifications, removeNotification } = inject("useNotification");
		return { notifications, removeNotification };
	},
};
</script>

<style scoped>
.notification-container {
	position: fixed;
	bottom: 20px;
	right: 20px;
	display: flex;
	flex-direction: column;
	z-index: 1000;
}
</style>