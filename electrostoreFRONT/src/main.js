import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import { router, i18n } from './helpers';

const app = createApp(App);

app.use(createPinia());
app.use(router);
app.use(i18n);

app.mount('#app');
