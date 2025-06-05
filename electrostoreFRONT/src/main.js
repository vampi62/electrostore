import { createApp } from "vue";
import { createPinia } from "pinia";

import App from "./App.vue";
import { router, i18n, useNotification } from "./helpers";

import { library } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import { faGithub } from "@fortawesome/free-brands-svg-icons";
import {
	faArrowLeft, faArrowRight,
	faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip,
	faTrash, faDownload, faFile, faFilter, faPenToSquare, faEye,
	faTruckLoading, faTruckMoving, faTruckRampBox,
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave,
	faRotateLeft, faRotateRight, faSearch, faCheck, faXmark, faBan,
} from "@fortawesome/free-solid-svg-icons";
import "./assets/tailwind.css";

import { Commentaire, FilterContainer, ModalDeleteConfirm, NotificationContainer, Tableau, TopButtonEditElement } from "./components";

const app = createApp(App);

library.add(
	faGithub,
	faArrowRight, faArrowLeft,
	faBox, faProjectDiagram, faShoppingCart, faCamera, faTags, faStore, faMicrochip,
	faTrash, faDownload, faFile, faFilter, faPenToSquare, faEye,
	faTruckLoading, faTruckMoving, faTruckRampBox,
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave,
	faRotateLeft, faRotateRight, faSearch, faCheck, faXmark, faBan,
);

app.component("font-awesome-icon", FontAwesomeIcon);
app.use(createPinia());
app.use(router);
app.use(i18n);
app.provide("useNotification", useNotification());
app.component("Commentaire", Commentaire);
app.component("FilterContainer", FilterContainer);
app.component("ModalDeleteConfirm", ModalDeleteConfirm);
app.component("NotificationContainer", NotificationContainer);
app.component("Tableau", Tableau);
app.component("TopButtonEditElement", TopButtonEditElement);

app.mount("#app");
