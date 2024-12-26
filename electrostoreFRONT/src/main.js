import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';
import { router, i18n } from './helpers';

import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { faGithub } from '@fortawesome/free-brands-svg-icons';
import { faArrowLeft, faArrowRight, faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip } from '@fortawesome/free-solid-svg-icons'
import './assets/tailwind.css';

const app = createApp(App);

library.add(faGithub, faArrowRight, faArrowLeft, faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip);

app.component('font-awesome-icon', FontAwesomeIcon)
app.use(createPinia());
app.use(router);
app.use(i18n);

app.mount('#app');
