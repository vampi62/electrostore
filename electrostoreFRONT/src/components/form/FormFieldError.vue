<template>
	<span class="text-red-500 h-5 w-full text-sm">{{ error || ' ' }}</span>
</template>

<script setup lang="ts">
defineProps<{ error?: string }>();
</script>
