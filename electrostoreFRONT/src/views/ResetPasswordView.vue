<script setup>
import { onMounted, ref, computed, inject } from 'vue';


const { addNotification } = inject('useNotification');

import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useRoute } from 'vue-router';
const route = useRoute();


import { useAuthStore, useConfigsStore } from '@/stores';
const configsStore = useConfigsStore();
const authStore = useAuthStore();

const token = route.query.token || '';
const email = route.query.email || '';


const schema = Yup.object().shape({
    email: Yup.string()
        .email(t('common.VResetPasswordEmailInvalid'))
        .required(t('common.VResetPasswordEmailRequired')),
        // 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character
    password: Yup.string()
        .matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, t('common.VResetPasswordPasswordRequirements'))
        .required(t('common.VResetPasswordPasswordRequired')),
    confirmPassword: Yup.string().oneOf([Yup.ref('password'), null], t('common.VResetPasswordPasswordMatch'))
        .matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, t('common.VResetPasswordPasswordRequirements'))
        .required(t('common.VResetPasswordPasswordConfirmRequired'))
});

function onSubmit(values, { setErrors }) {
    const { email, token, password } = values;
    return authStore.resetPassword(email, token, password)
        .catch(error => setErrors({ apiError: error }))
        .then(() => setErrors({ apiConfirm: t('common.VResetPasswordSuccess') }));
}
</script>

<template>
    <div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
        <h2 class="text-2xl font-bold mb-4">{{ $t('common.VResetPasswordTitle') }}</h2>

        <div v-if="configsStore.configs.loading" class="flex justify-center items-center">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" role="status">
                <span class="sr-only">{{ $t('common.VResetPasswordLoading') }}</span>
            </div>
        </div>

        <div v-else-if="configsStore.configs.smtp_enabled">
            <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
                <!-- Email Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">{{ $t('common.VResetPasswordEmail') }}</label>
                    <Field name="email" type="email"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 bg-gray-100 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.email }" :value="email" disabled />
                    <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.email }}</div>
                </div>

                <!-- Hidden Token Field -->
                <Field name="token" type="hidden" :value="token" />

                <!-- New Password Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">{{ $t('common.VResetPasswordNewPassword') }}</label>
                    <Field name="password" type="password"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.password }" />
                    <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.password }}</div>
                </div>

                <!-- Confirm New Password Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">{{ $t('common.VResetPasswordConfirmNewPassword') }}</label>
                    <Field name="confirmPassword" type="password"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.confirmPassword }" />
                    <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.confirmPassword }}
                    </div>
                </div>

                <!-- Submit Button -->
                <div class="mb-4">
                    <button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
                        :disabled="isSubmitting">
                        <span v-show="isSubmitting"
                            class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
                        {{ $t('common.VResetPasswordSubmit') }}
                    </button>
                </div>

                <!-- API Error/Confirmation Messages -->
                <div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
                <div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ errors.apiConfirm
                    }}</div>
            </Form>
        </div>

        <!-- SMTP Disabled Alert -->
        <div v-else class="bg-red-100 text-red-600 p-3 rounded">{{ $t('common.VResetPasswordSmtpDisabled') }}</div>

        <!-- Link to Login -->
        <div class="mt-4">
            <RouterLink to="/login" class="text-blue-500 hover:underline">{{ $t('common.VResetPasswordLoginLink') }}</RouterLink>
        </div>
    </div>
</template>
