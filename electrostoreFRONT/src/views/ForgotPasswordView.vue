<script setup>
import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useConfigsStore } from "@/stores";
const configsStore = useConfigsStore();
const authStore = useAuthStore();

const schema = Yup.object().shape({
	email: Yup.string()
		.email(t("common.VForgotPasswordEmailInvalid"))
		.required(t("common.VForgotPasswordEmailRequired")),
});

function onSubmit(values, { setErrors }) {
	const { email } = values;
	return authStore.forgotPassword(email)
		.catch((error) => setErrors({ apiError: error }))
		.then(() => setErrors({ apiConfirm: t("common.VForgotPasswordEmailSent") }));
}
</script>

<template>
	<div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('common.VForgotPasswordTitle') }}</h2>
		<!-- Loading Spinner -->
		<div v-if="configsStore.configs.loading" class="flex items-center justify-center my-4">
			<div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin">
				<span class="sr-only">{{ $t('common.VForgotPasswordLoading') }}</span>
			</div>
		</div>

		<!-- Form Section -->
		<div v-else-if="configsStore.configs.smtp_enabled">
			<Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
				<div class="mb-4">
					<label class="block text-gray-700" for="email">{{ $t('common.VForgotPasswordEmail') }}</label>
					<Field name="email" type="email"
						class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
						:class="{ 'border-red-500': errors.email }" />
					<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.email }}</div>
				</div>
				<div class="mb-4">
					<button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
						:disabled="isSubmitting">
						<span v-show="isSubmitting"
							class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
						{{ $t('common.VForgotPasswordSubmit') }}
					</button>
				</div>
				<!-- Error/Success Messages -->
				<div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">
					{{ errors.apiError }}
				</div>
				<div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">
					{{ errors.apiConfirm }}
				</div>
			</Form>
		</div>

		<!-- SMTP Disabled Message -->
		<div v-else class="bg-red-100 text-red-600 p-3 rounded">{{ $t('common.VForgotPasswordSmtpDisabled') }}</div>

		<!-- Links -->
		<div class="mt-4">
			<RouterLink to="/login" class="text-blue-500 hover:underline">{{ $t('common.VForgotPasswordLoginLink') }}
			</RouterLink>
			<RouterLink to="/register" class="ml-4 text-blue-500 hover:underline">
				{{ $t('common.VForgotPasswordRegisterLink') }}
			</RouterLink>
		</div>
	</div>
</template>
