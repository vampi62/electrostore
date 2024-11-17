<script setup>
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useAuthStore, useConfigsStore } from '@/stores';
const configsStore = useConfigsStore();
const authStore = useAuthStore();

import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

const schema = Yup.object().shape({
    email: Yup.string().email().required(t('emailRequired'))
});

function onSubmit(values, { setErrors }) {
    const { email } = values;
    return authStore.forgotPassword(email)
        .catch(error => setErrors({ apiError: error["email_user"] }))
        .then(() => setErrors({ apiConfirm: 'Email sent' }));
}
</script>

<template>
    <div>
        <h2 class="text-2xl font-bold mb-4">{{ $t('forgotPassword') }}</h2>
    </div>

    <!-- Loading Spinner -->
    <div v-if="configsStore.configs.loading" class="flex items-center justify-center my-4">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" role="status">
            <span class="sr-only">Loading...</span>
        </div>
    </div>

    <!-- Form Section -->
    <div v-else-if="configsStore.configs.smtp_enabled">
        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
            <div class="mb-4">
                <label class="block text-gray-700">{{ $t('email') }}</label>
                <Field name="email" type="email"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.email }" />
                <div v-if="errors.email" class="text-red-500 text-sm mt-1">{{ errors.email }}</div>
            </div>
            <div class="mb-4">
                <button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
                    :disabled="isSubmitting">
                    <span v-show="isSubmitting"
                        class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
                    Envoyer
                </button>
            </div>
            <!-- Error/Success Messages -->
            <div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
            <div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ errors.apiConfirm }}</div>
        </Form>
    </div>

    <!-- SMTP Disabled Message -->
    <div v-else>
        <div class="bg-red-100 text-red-600 p-3 rounded">{{ $t('smtpDisabled') }}</div>
    </div>

    <!-- Links -->
    <div class="mt-4">
        <router-link to="/login" class="text-blue-500 hover:underline">{{ $t('login') }}</router-link>
        <router-link to="/register" class="ml-4 text-blue-500 hover:underline">{{ $t('register') }}</router-link>
    </div>
</template>
