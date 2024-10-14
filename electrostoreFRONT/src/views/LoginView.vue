<script setup>
import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';

import { useAuthStore } from '@/stores';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

const schema = Yup.object().shape({
    email: Yup.string().email().required($t('emailRequired')),
    password: Yup.string().required('Password is required')
});

function onSubmit(values, { setErrors }) {
    const authStore = useAuthStore();
    const { email, password } = values;

    return authStore.login(email, password)
        .catch(error => setErrors({ apiError: error }));
}
</script>

<template>
    <div>
        <h2>{{ $t('login') }}</h2>
        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
            <div class="form-group">
                <label>{{ $t('email') }}</label>
                <Field name="email" type="email" class="form-control" :class="{ 'is-invalid': errors.email }" />
                <div class="invalid-feedback">{{errors.email}}</div>
            </div>            
            <div class="form-group">
                <label>Password</label>
                <Field name="password" type="password" class="form-control" :class="{ 'is-invalid': errors.password }" />
                <div class="invalid-feedback">{{errors.password}}</div>
            </div>            
            <div class="form-group">
                <button class="btn btn-primary" :disabled="isSubmitting">
                    <span v-show="isSubmitting" class="spinner-border spinner-border-sm mr-1"></span>
                    {{ $t('login') }}
                </button>
            </div>
            <div v-if="errors.apiError" class="alert alert-danger mt-3 mb-0">{{errors.apiError}}</div>
        </Form>
        <div>
            <router-link to="/register">{{ $t('register') }}</router-link>
            <router-link to="/forgot-password" class="ml-3">{{ $t('forgotPassword') }}</router-link>
        </div>
    </div>
</template>
