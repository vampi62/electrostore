// ***********************************************************
// This file is processed and loaded automatically before your test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Mock Vue i18n
Cypress.on('window:before:load', (win) => {
  // Create a mock for the Vue i18n t() function
  win.t = (key) => {
    // Split the key into namespace and actual key
    const parts = key.split('.');
    const namespace = parts[0];
    const translationKey = parts[1];

    // Define translations for the tests
    // Add more translations as needed for your tests
    const translations = {
      common: {
        VLoginEmailRequired: "L'adresse e-mail est obligatoire",
        VLoginPasswordRequired: "Le mot de passe est obligatoire",
        VLoginEmailInvalid: "l'adresse e-mail invalide",
        VLoginError: "Identifiants incorrects",
        VLoginTitle: "Connexion",
        VLoginEmail: "Adresse e-mail",
        VLoginPassword: "Mot de passe",
        VLoginSubmit: "Se connecter",
        VLoginForgotPasswordLink: "Mot de passe oublié ?",
        VLoginRegisterLink: "Créer un compte"
      }
    };

    // Return the translation if it exists, otherwise return the key
    return translations[namespace] && translations[namespace][translationKey]
      ? translations[namespace][translationKey]
      : key;
  };

  // Mock the useI18n function that components use
  win.useI18n = () => {
    return {
      t: win.t,
      locale: { value: 'fr' }
    };
  };

  // Add the mock to Vue prototype for components that use this.$t
  if (win.Vue) {
    win.Vue.prototype.$t = win.t;
  }
});

// Import commands.js if you have it
// import './commands'