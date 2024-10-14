<script setup>
import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

import { useAuthStore } from '@/stores';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

const schema = Yup.object().shape({
    email: Yup.string().email().required($t('emailRequired'))
});

function onSubmit(values, { setErrors }) {
    const authStore = useAuthStore();
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
    <div>
        <router-link to="/login">{{ $t('login') }}</router-link>
        <router-link to="/register" class="ml-3">{{ $t('register') }}</router-link>
    </div>
</template>
