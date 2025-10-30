<script setup>
import { onMounted, ref } from "vue";
import { useAuthStore } from "@/stores";

const authStore = useAuthStore();

const loading = ref(true);
const error = ref(null);

onMounted(async() => {
	try {
		await authStore.handleSSOCallback();
		loading.value = false;
	} catch (err) {
		error.value = err;
		loading.value = false;
		setTimeout(() => {
			window.location.href = "/login";
		}, 3000);
	}
});
</script>

<template>
	<div class="max-w-md mx-auto bg-white p-6 rounded shadow text-center">
		<div v-if="loading">
			<h2 class="text-2xl font-bold mb-4">{{ $t('common.VCallbackProcessingTitle') }}</h2>
			<div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto"></div>
			<p class="mt-4 text-gray-600">{{ $t('common.VCallbackWaiting') }}</p>
		</div>
		
		<div v-else-if="error" class="text-red-600">
			<h2 class="text-2xl font-bold mb-4">{{ $t('common.VCallbackErrorTitle') }}</h2>
			<p class="mb-4">{{ error }}</p>
			<p class="text-sm text-gray-600">{{ $t('common.VCallbackRedirecting') }}</p>
		</div>
		
		<div v-else>
			<h2 class="text-2xl font-bold mb-4 text-green-600">{{ $t('common.VCallbackSuccessTitle') }}</h2>
			<p class="text-gray-600">{{ $t('common.VCallbackRedirecting') }}</p>
		</div>
	</div>
</template>