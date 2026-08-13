import { createApp, defineAsyncComponent } from "vue";
import { createPinia } from "pinia";

import App from "./App.vue";
import { useNotification } from "./composables";
import { i18n } from "./plugins";
import router from "./router";

import { library } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { faGithub } from "@fortawesome/free-brands-svg-icons";
import {
	faArrowLeft, faArrowRight,
	faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip,
	faTrash, faDownload, faFile, faFilter, faPenToSquare, faEye,
	faTruckLoading, faTruckMoving, faTruckRampBox,
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave, faSpinner, faClock,
	faRotateLeft, faRotateRight, faSearch, faCheck, faXmark, faBan,
} from "@fortawesome/free-solid-svg-icons";
import "./assets/tailwind.css";

const app = createApp(App);

library.add(
	faGithub,
	faArrowRight, faArrowLeft,
	faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip,
	faTrash, faDownload, faFile, faFilter, faPenToSquare, faEye,
	faTruckLoading, faTruckMoving, faTruckRampBox,
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave, faSpinner, faClock,
	faRotateLeft, faRotateRight, faSearch, faCheck, faXmark, faBan,
);

app.component("font-awesome-icon", FontAwesomeIcon);
app.use(createPinia());
app.use(router);
app.use(i18n);
app.provide("useNotification", useNotification());

const components = import.meta.glob("./components/*.vue");
for (const path in components) {
	const componentName = path.split("/").pop().replace(/\.vue$/, "");
	app.component(componentName, defineAsyncComponent(components[path]));
}

app.mount("#app");
