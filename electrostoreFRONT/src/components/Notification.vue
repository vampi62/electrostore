<template>
  <div class="notification" :class="type">
    <p>{{ message }}</p>
    <button @click="close">X</button>
  </div>
</template>

<script>
export default {
  props: {
    message: {
      type: String,
      required: true,
    },
    type: {
      type: String,
      default: 'info', // 'info', 'success', 'error', etc.
    },
    id: {
      type: [String, Number],
      required: true,
    },
  },
  emits: ['remove'],
  methods: {
    close() {
      this.$emit('remove', this.id);
    },
  },
};
</script>

<style scoped>
.notification {
  background: #f0f0f0;
  padding: 10px 20px;
  margin-bottom: 10px;
  border-radius: 5px;
  box-shadow: 0px 2px 5px rgba(0, 0, 0, 0.1);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notification.info {
  border-left: 5px solid #2196f3;
}

.notification.success {
  border-left: 5px solid #4caf50;
}

.notification.error {
  border-left: 5px solid #f44336;
}
</style>