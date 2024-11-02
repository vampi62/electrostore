import { createI18n } from 'vue-i18n';

const messages = {
  en: {
    login: 'Login',
    forgotPassword: 'Forgot your password?',
    register: 'Register',
    email: 'Email',
    emailRequired: 'Email is required',
    smtpDisabled: 'SMTP is disabled',
  },
  fr: {
    login: 'Connexion',
    forgotPassword: 'Mot de passe oublié ?',
    register: 'inscription',
    email: 'Email',
    emailRequired: 'Email est requis',
    smtpDisabled: 'SMTP est désactivé',
  },
  es: {
    login: 'Iniciar sesión',
    forgotPassword: '¿Olvidaste tu contraseña?',
    register: 'Registrarse',
    email: 'Correo electrónico',
    emailRequired: 'Correo electrónico es obligatorio',
    smtpDisabled: 'SMTP está desactivado',
  },
};

export const i18n = createI18n({
  locale: 'fr', // défaut
  fallbackLocale: 'fr', // si traduction est manquante
  messages,
});
