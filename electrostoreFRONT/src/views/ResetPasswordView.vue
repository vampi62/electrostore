<script setup>
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useAuthStore, useConfigsStore } from '@/stores';
const configsStore = useConfigsStore();
const authStore = useAuthStore();

import { useRoute } from 'vue-router';
const route = useRoute();
const token = route.query.token || '';
const email = route.query.email || '';

import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

const schema = Yup.object().shape({
    email: Yup.string().email().required(t('emailRequired')),
    password: Yup.string().matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, 'Password must be at least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character').required('Password is required'),
    confirmPassword: Yup.string().oneOf([Yup.ref('password'), null], 'Passwords must match').matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, 'Password must be at least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character').required('Confirm Password is required')
});

function onSubmit(values, { setErrors }) {
    const { email, token, password } = values;
    return authStore.resetPassword(email, token, password)
        .catch(error => setErrors({ apiError: error }))
        .then(() => setErrors({ apiConfirm: 'Password reset' }));
}
</script>

<template>
    <div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
        <h2 class="text-2xl font-bold mb-4">Reset Password</h2>

        <div v-if="configsStore.configs.loading" class="flex justify-center items-center">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" role="status">
                <span class="sr-only">Loading...</span>
            </div>
        </div>

        <div v-else-if="configsStore.configs.smtp_enabled">
            <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
                <!-- Email Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">{{ $t('email') }}</label>
                    <Field
                        name="email"
                        type="email"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 bg-gray-100 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.email }"
                        :value="email"
                        disabled
                    />
                    <div v-if="errors.email" class="text-red-500 text-sm mt-1">{{ errors.email }}</div>
                </div>

                <!-- Hidden Token Field -->
                <Field name="token" type="hidden" :value="token" />

                <!-- New Password Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">New Password</label>
                    <Field
                        name="password"
                        type="password"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.password }"
                    />
                    <div v-if="errors.password" class="text-red-500 text-sm mt-1">{{ errors.password }}</div>
                </div>

                <!-- Confirm New Password Field -->
                <div class="mb-4">
                    <label class="block text-gray-700">Confirm New Password</label>
                    <Field
                        name="confirmPassword"
                        type="password"
                        class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                        :class="{ 'border-red-500': errors.confirmPassword }"
                    />
                    <div v-if="errors.confirmPassword" class="text-red-500 text-sm mt-1">{{ errors.confirmPassword }}</div>
                </div>

                <!-- Submit Button -->
                <div class="mb-4">
                    <button
                        class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
                        :disabled="isSubmitting"
                    >
                        <span
                            v-show="isSubmitting"
                            class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"
                        ></span>
                        Envoyer
                    </button>
                </div>

                <!-- API Error/Confirmation Messages -->
                <div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
                <div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ errors.apiConfirm }}</div>
            </Form>
        </div>

        <!-- SMTP Disabled Alert -->
        <div v-else class="bg-red-100 text-red-600 p-3 rounded">{{ $t('smtpDisabled') }}</div>

        <!-- Link to Login -->
        <div class="mt-4">
            <router-link to="/login" class="text-blue-500 hover:underline">Login</router-link>
        </div>
    </div>
</template>
