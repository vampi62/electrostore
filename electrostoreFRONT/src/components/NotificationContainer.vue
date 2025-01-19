<template>
    <div class="notification-container">
        <Notification v-for="(notification, index) in notifications" :key="notification.id" :id="notification.id"
            :message="notification.message" :type="notification.type" @remove="removeNotification" />
    </div>
</template>

<script>
import Notification from './Notification.vue';
import { inject } from 'vue';

export default {
    components: { Notification },
    setup() {
      const { notifications, removeNotification } = inject('useNotification');
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