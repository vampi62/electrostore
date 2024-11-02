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
    <div>
        <h2>{{ $t('register') }}</h2>
    </div>
    <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }">
        <div class="form-group">
            <label>{{ $t('email') }}</label>
            <Field name="email" type="email" class="form-control" :class="{ 'is-invalid': errors.email }" />
            <div class="invalid-feedback">{{errors.email}}</div>
        </div>
        <div class="form-group">
            <label>Prénom</label>
            <Field name="firstName" type="text" class="form-control" :class="{ 'is-invalid': errors.firstName }" />
            <div class="invalid-feedback">{{errors.firstName}}</div>
        </div>
        <div class="form-group">
            <label>Nom</label>
            <Field name="lastName" type="text" class="form-control" :class="{ 'is-invalid': errors.lastName }" />
            <div class="invalid-feedback">{{errors.lastName}}</div>
        </div>
        <div class="form-group">
            <label>Password</label>
            <Field name="password" type="password" class="form-control" :class="{ 'is-invalid': errors.password }" />
            <div class="invalid-feedback">{{errors.password}}</div>
        </div>
        <div class="form-group">
            <label>Confirm Password</label>
            <Field name="confirmPassword" type="password" class="form-control" :class="{ 'is-invalid': errors.confirmPassword }" />
            <div class="invalid-feedback">{{errors.confirmPassword}}</div>
        </div>
        <div class="form-group">
            <button class="btn btn-primary" :disabled="isSubmitting">
                <span v-show="isSubmitting" class="spinner-border spinner-border-sm mr-1"></span>
                {{ $t('register') }}
            </button>
        </div>
        <div v-if="errors.apiError" class="alert alert-danger mt-3 mb-0">{{errors.apiError}}</div>
        <div v-if="errors.apiConfirm" class="alert alert-success mt-3 mb-0">{{errors.apiConfirm}}</div>
    </Form>
    <div>
        <router-link to="/login">{{ $t('login') }}</router-link>
    </div>
</template>
