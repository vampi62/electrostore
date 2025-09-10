<template>
	<div class="fixed bottom-5 right-5 flex flex-col z-[1000]">
		<Notification v-for="(notification) in notifications" :key="notification.id" :id="notification.id"
			:message="notification.message" :type="notification.type" :i18n="notification.i18n"
			@remove="removeNotification" />
	</div>
</template>

<script>
import { inject, defineAsyncComponent } from "vue";

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