<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";

import { useAuthStore, useConfigsStore } from "@/stores";

const { t } = useI18n();
const route = useRoute();
const configsStore = useConfigsStore();
const authStore = useAuthStore();

const token = String(route.query.token ?? "");
const email = String(route.query.email ?? "");

const status = ref<"loading" | "success" | "error">("loading");
const errorMessage = ref("");
const resending = ref(false);
const resendConfirm = ref("");
const resendError = ref("");

async function verify() {
	if (!token || !email) {
		errorMessage.value = t("common.VVerifyEmailLinkInvalid");
		status.value = "error";
		return;
	}
	try {
		await authStore.verifyEmail(email, token);
		status.value = "success";
	} catch (error) {
		errorMessage.value = error instanceof Error ? error.message : String(error);
		status.value = "error";
	}
}

async function resend() {
	resending.value = true;
	resendConfirm.value = "";
	resendError.value = "";
	try {
		await authStore.resendVerification(email);
		resendConfirm.value = t("common.VVerifyEmailResendSent");
	} catch (error) {
		resendError.value = error instanceof Error ? error.message : String(error);
	} finally {
		resending.value = false;
	}
}

onMounted(verify);
</script>

<template>
	<div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('common.VVerifyEmailTitle') }}</h2>

		<!-- Loading Spinner -->
		<div v-if="status === 'loading'" class="flex items-center justify-center my-4">
			<div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin">
				<span class="sr-only">{{ $t('common.VVerifyEmailLoading') }}</span>
			</div>
		</div>

		<!-- Success -->
		<div v-else-if="status === 'success'" class="bg-green-100 text-green-600 p-3 rounded">
			{{ $t('common.VVerifyEmailSuccess') }}
		</div>

		<!-- Error -->
		<div v-else>
			<div class="bg-red-100 text-red-600 p-3 rounded">
				<p>{{ $t('common.VVerifyEmailError') }}</p>
				<p v-if="errorMessage" class="text-sm mt-1">{{ errorMessage }}</p>
			</div>

			<!-- Ask for a new verification email -->
			<div v-if="email && !configsStore.configs.loading && configsStore.getStatusByKey('notif_smtp')" class="mt-4">
				<button type="button" class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
					:disabled="resending" @click="resend">
					<span v-show="resending"
						class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
					{{ $t('common.VVerifyEmailResend') }}
				</button>
				<div v-if="resendError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ resendError }}</div>
				<div v-if="resendConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ resendConfirm }}</div>
			</div>
		</div>

		<!-- Link to Login / Home -->
		<div class="mt-4">
			<RouterLink v-if="authStore.user" to="/" class="text-blue-500 hover:underline">{{ $t('common.VVerifyEmailHomeLink') }}</RouterLink>
			<RouterLink v-else to="/login" class="text-blue-500 hover:underline">{{ $t('common.VVerifyEmailLoginLink') }}</RouterLink>
		</div>
	</div>
</template>
