<script setup>
import { ref } from "vue";
import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useConfigsStore } from "@/stores";
const configsStore = useConfigsStore();
const authStore = useAuthStore();

const showPassword = ref(false);

const schema = Yup.object().shape({
	email: Yup.string()
		.email(t("common.VLoginEmailInvalid"))
		.required(t("common.VLoginEmailRequired")),
	password: Yup.string()
		.required(t("common.VLoginPasswordRequired")),
});

function onSubmit(values, { setErrors }) {
	const { email, password } = values;
	return authStore.login(email, password)
		.catch((error) => setErrors({ apiError: t("common.VLoginError") }));
}
</script>

<template>
	<div class="max-w-md mx-auto bg-white p-6 rounded shadow">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('common.VLoginTitle') }}</h2>

		<Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
			<!-- Email Field -->
			<div class="mb-4">
				<label class="block text-gray-700" for="email">{{ $t('common.VLoginEmail') }}</label>
				<Field name="email" type="email"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.email }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.email }}</div>
			</div>

			<!-- Password Field -->
			<div class="mb-4">
				<label class="block text-gray-700" for="password">{{ $t('common.VLoginPassword') }}</label>
				<div class="relative">
					<Field name="password" :type="showPassword ? 'text' : 'password'"
						class="border border-gray-300 rounded w-full px-3 py-2 pr-10 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
						:class="{ 'border-red-500': errors.password }" />
					<button type="button" @mouseup="showPassword = false" @mousedown="showPassword = true"
						class="absolute inset-y-0 right-0 pr-3 mt-1 flex items-center text-gray-600 hover:text-gray-800">
						<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
						</svg>
					</button>
				</div>
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.password }}</div>
			</div>

			<!-- Submit Button -->
			<div class="mb-4 flex justify-between items-center">
				<button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
					:disabled="isSubmitting" type="submit">
					<span v-show="isSubmitting"
						class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
					{{ $t('common.VLoginSubmit') }}
				</button>
			</div>

			<!-- API Error Message -->
			<div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
		</Form>
			
		<!-- horizontal separation-->
		<hr class="my-4" />

		<!-- available SSO providers in config -->
		<div v-if="configsStore.configs.loading" class="flex items-center justify-center my-4">
			<div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin">
				<span class="sr-only">{{ $t('common.VLoginLoadingSSO') }}</span>
			</div>
		</div>
		<div v-else>
			<div v-if="configsStore.configs.sso_available_providers.length > 0" class="space-y-2 mb-4">
				<div v-for="provider in configsStore.configs.sso_available_providers" :key="provider.provider">
					<button @click="authStore.loginSSO(provider.provider)"
						class="w-full flex items-center justify-center border border-gray-300 rounded px-4 py-2 hover:bg-gray-100">
						<img :src="provider.icon_url" :alt="provider.display_name" class="h-6 w-6 mr-2" />
						<span>{{ $t('common.VLoginSSOLoginWith', { provider: provider.display_name }) }}</span>
					</button>
				</div>
			</div>
			<div v-else class="text-gray-500 italic mb-4">{{ $t('common.VLoginNoSSOProviders') }}</div>
		</div>

		<!-- Links -->
		<div class="mt-4 flex justify-between">
			<RouterLink to="/register" class="text-blue-500 hover:underline">{{ $t('common.VLoginRegisterLink') }}
			</RouterLink>
			<RouterLink to="/forgot-password" class="ml-4 text-blue-500 hover:underline">
				{{ $t('common.VLoginForgotPasswordLink') }}
			</RouterLink>
		</div>
	</div>
</template>
