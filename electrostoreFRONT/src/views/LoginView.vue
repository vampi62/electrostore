<script setup>
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useAuthStore } from '@/stores';
const authStore = useAuthStore();

import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

const schema = Yup.object().shape({
    email: Yup.string().email().required(t('emailRequired')),
    password: Yup.string().required('Password is required')
});

function onSubmit(values, { setErrors }) {
    const { email, password } = values;
    return authStore.login(email, password)
        .catch(error => setErrors({ apiError: error }));
}
</script>

<template>
    <div class="max-w-md mx-auto bg-white p-6 rounded shadow">
        <h2 class="text-2xl font-bold mb-4">{{ $t('login') }}</h2>

        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
            <!-- Email Field -->
            <div class="mb-4">
                <label class="block text-gray-700">{{ $t('email') }}</label>
                <Field name="email" type="email"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.email }" />
                <div v-if="errors.email" class="text-red-500 text-sm mt-1">{{ errors.email }}</div>
            </div>

            <!-- Password Field -->
            <div class="mb-4">
                <label class="block text-gray-700">Password</label>
                <Field name="password" type="password"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.password }" />
                <div v-if="errors.password" class="text-red-500 text-sm mt-1">{{ errors.password }}</div>
            </div>

            <!-- Submit Button -->
            <div class="mb-4">
                <button class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 disabled:bg-blue-400"
                    :disabled="isSubmitting">
                    <span v-show="isSubmitting"
                        class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>
                    {{ $t('login') }}
                </button>
            </div>

            <!-- API Error Message -->
            <div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
        </Form>

        <!-- Links -->
        <div class="mt-4">
            <router-link to="/register" class="text-blue-500 hover:underline">{{ $t('register') }}</router-link>
            <router-link to="/forgot-password" class="ml-4 text-blue-500 hover:underline">{{ $t('forgotPassword')
                }}</router-link>
        </div>
    </div>
</template>
