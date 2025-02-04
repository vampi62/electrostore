<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore } from "@/stores";
const authStore = useAuthStore();

const schema = Yup.object().shape({
	email: Yup.string()
		.email(t("common.VRegisterEmailInvalid"))
		.required(t("common.VRegisterEmailRequired")),
	firstName: Yup.string()
		.required(t("common.VRegisterFirstNameRequired")),
	lastName: Yup.string()
		.required(t("common.VRegisterLastNameRequired")),
	// 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character
	password: Yup.string()
		.matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, t("common.VRegisterPasswordRequirements"))
		.required(t("common.VRegisterPasswordRequired")),
	confirmPassword: Yup.string().oneOf([Yup.ref("password"), null], t("common.VRegisterPasswordMatch"))
		.matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, t("common.VRegisterPasswordRequirements"))
		.required(t("common.VRegisterConfirmPasswordRequired")),
});

function onSubmit(values, { setErrors }) {
	const { email, firstName, lastName, password } = values;
	return authStore.register(email, password, firstName, lastName)
		.catch((errors) => setErrors({ apiError: errors }))
		.then(() => setErrors({ apiConfirm: t("common.VRegisterSuccess") }));
}
</script>

<template>
	<div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
		<h2 class="text-2xl font-bold mb-4">{{ $t('common.VRegisterTitle') }}</h2>

		<Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
			<!-- Email Field -->
			<div class="mb-4">
				<label class="block text-gray-700">{{ $t('common.VRegisterEmail') }}</label>
				<Field name="email" type="email"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.email }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.email }}</div>
			</div>

			<!-- First Name Field -->
			<div class="mb-4">
				<label class="block text-gray-700">{{ $t('common.VRegisterFirstName') }}</label>
				<Field name="firstName" type="text"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.firstName }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.firstName }}</div>
			</div>

			<!-- Last Name Field -->
			<div class="mb-4">
				<label class="block text-gray-700">{{ $t('common.VRegisterLastName') }}</label>
				<Field name="lastName" type="text"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.lastName }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.lastName }}</div>
			</div>

			<!-- Password Field -->
			<div class="mb-4">
				<label class="block text-gray-700">{{ $t('common.VRegisterPassword') }}</label>
				<Field name="password" type="password"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.password }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.password }}</div>
			</div>

			<!-- Confirm Password Field -->
			<div class="mb-4">
				<label class="block text-gray-700">{{ $t('common.VRegisterConfirmPassword') }}</label>
				<Field name="confirmPassword" type="password"
					class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
					:class="{ 'border-red-500': errors.confirmPassword }" />
				<div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.confirmPassword }}</div>
			</div>

			<!-- Submit Button -->
			<div class="mb-4">
				<button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
					:disabled="isSubmitting">
					<span v-show="isSubmitting"
						class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
					{{ $t('common.VRegisterSubmit') }}
				</button>
			</div>

			<!-- API Error/Success Messages -->
			<div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
			<div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ errors.apiConfirm }}
			</div>
		</Form>

		<!-- Link to Login -->
		<div class="mt-4">
			<RouterLink to="/login" class="text-blue-500 hover:underline">{{ $t('common.VRegisterLoginLink') }}
			</RouterLink>
		</div>
	</div>
</template>
