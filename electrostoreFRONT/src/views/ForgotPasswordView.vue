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
        <h2>{{ $t('forgotPassword') }}</h2>
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
                <Field name="email" type="email" class="form-control" :class="{ 'is-invalid': errors.email }" />
                <div class="invalid-feedback">{{errors.email}}</div>
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
        <router-link to="/login">{{ $t('login') }}</router-link>
        <router-link to="/register" class="ml-3">{{ $t('register') }}</router-link>
    </div>
</template>
