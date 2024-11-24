<script setup>
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { useAuthStore } from '@/stores';
const authStore = useAuthStore();

import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

const schema = Yup.object().shape({
    email: Yup.string().email().required(t('emailRequired')),
    firstName: Yup.string().required('First Name is required'),
    lastName: Yup.string().required('Last Name is required'),
    // 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character
    password: Yup.string().matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, 'Password must be at least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character').required('Password is required'),
    confirmPassword: Yup.string().oneOf([Yup.ref('password'), null], 'Passwords must match').matches(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$/, 'Password must be at least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character').required('Confirm Password is required')
});

function onSubmit(values, { setErrors }) {
    const { email, firstName, lastName, password } = values;
    return authStore.register(email, password, firstName, lastName)
        .catch(errors => setErrors({ apiError: errors }))
        .then(() => setErrors({ apiConfirm: 'User registered' }));
}
</script>

<template>
    <div class="max-w-lg mx-auto bg-white p-6 rounded shadow">
        <h2 class="text-2xl font-bold mb-4">{{ $t('register') }}</h2>

        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
            <!-- Email Field -->
            <div class="mb-4">
                <label class="block text-gray-700">{{ $t('email') }}</label>
                <Field name="email" type="email"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.email }" />
                <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.email }}</div>
            </div>

            <!-- First Name Field -->
            <div class="mb-4">
                <label class="block text-gray-700">Prénom</label>
                <Field name="firstName" type="text"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.firstName }" />
                <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.firstName }}</div>
            </div>

            <!-- Last Name Field -->
            <div class="mb-4">
                <label class="block text-gray-700">Nom</label>
                <Field name="lastName" type="text"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.lastName }" />
                <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.lastName }}</div>
            </div>

            <!-- Password Field -->
            <div class="mb-4">
                <label class="block text-gray-700">Password</label>
                <Field name="password" type="password"
                    class="border border-gray-300 rounded w-full px-3 py-2 mt-1 focus:outline-none focus:ring focus:ring-blue-300"
                    :class="{ 'border-red-500': errors.password }" />
                <div class="text-red-500 text-sm mt-1 min-h-5">{{ errors.password }}</div>
            </div>

            <!-- Confirm Password Field -->
            <div class="mb-4">
                <label class="block text-gray-700">Confirm Password</label>
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
                    {{ $t('register') }}
                </button>
            </div>

            <!-- API Error/Success Messages -->
            <div v-if="errors.apiError" class="bg-red-100 text-red-600 p-3 rounded mt-3">{{ errors.apiError }}</div>
            <div v-if="errors.apiConfirm" class="bg-green-100 text-green-600 p-3 rounded mt-3">{{ errors.apiConfirm }}
            </div>
        </Form>

        <!-- Link to Login -->
        <div class="mt-4">
            <router-link to="/login" class="text-blue-500 hover:underline">{{ $t('login') }}</router-link>
        </div>
    </div>
</template>
