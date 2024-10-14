import { createI18n } from 'vue-i18n';

const messages = {
  en: {
    login: 'Login',
    forgotPassword: 'Forgot your password?',
    register: 'Register',
    email: 'Email',
    emailRequired: 'Email is required',
  },
  fr: {
    login: 'Connexion',
    forgotPassword: 'Mot de passe oublié ?',
    register: 'inscription',
    email: 'Email',
    emailRequired: 'Email est requis',
  },
  es: {
    login: 'Iniciar sesión',
    forgotPassword: '¿Olvidaste tu contraseña?',
    register: 'Registrarse',
    email: 'Correo electrónico',
    emailRequired: 'Correo electrónico es obligatorio',
  },
};

export const i18n = createI18n({
  locale: 'fr', // défaut
  fallbackLocale: 'fr', // si traduction est manquante
  messages,
});
