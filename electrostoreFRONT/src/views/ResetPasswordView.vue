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
    <div>
        <h2>Reset Password</h2>
    </div>
    <div v-if="configsStore.configs.loading">
        <div class="spinner-border text-primary" role="status">
            <span class="sr-only">Loading...</span>
        </div>
    </div>
    <div v-else-if="configsStore.configs.smtp_enabled">
        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
            <div class="form-group">
                <label>{{ $t('email') }}</label>
                <Field name="email" type="email" class="form-control" :class="{ 'is-invalid': errors.email }" :value="email" disabled />
                <div class="invalid-feedback">{{errors.email}}</div>
            </div>
            <Field name="token" type="hidden" :value="token" />
            <div class="form-group">
                <label>New Password</label>
                <Field name="password" type="password" class="form-control" :class="{ 'is-invalid': errors.password }" />
                <div class="invalid-feedback">{{errors.password}}</div>
            </div>
            <div class="form-group">
                <label>Confirm New Password</label>
                <Field name="confirmPassword" type="password" class="form-control" :class="{ 'is-invalid': errors.confirmPassword }" />
                <div class="invalid-feedback">{{errors.confirmPassword}}</div>
            </div>
            <div class="form-group">
                <button class="btn btn-primary" :disabled="isSubmitting">
                    <span v-show="isSubmitting" class="spinner-border spinner-border-sm mr-1"></span>
                    Envoyer
                </button>
            </div>
            <div v-if="errors.apiError" class="alert alert-danger mt-3 mb-0">{{errors.apiError}}</div>
            <div v-if="errors.apiConfirm" class="alert alert-success mt-3 mb-0">{{errors.apiConfirm}}</div>
        </Form>
    </div>
    <div v-else>
        <div class="alert alert-danger">{{ $t('smtpDisabled') }}</div>
    </div>
    <div>
        <router-link to="/login">Login</router-link>
    </div>
</template>
