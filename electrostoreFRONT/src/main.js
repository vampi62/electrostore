import { createApp, defineAsyncComponent } from "vue";
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
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave, faSpinner,
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
	faSort, faSortUp, faSortDown, faPlus, faMinus, faSave, faSpinner,
	faRotateLeft, faRotateRight, faSearch, faCheck, faXmark, faBan,
);

app.component("font-awesome-icon", FontAwesomeIcon);
app.use(createPinia());
app.use(router);
app.use(i18n);
app.provide("useNotification", useNotification());
app.component(
	"CollapsibleSection",
	defineAsyncComponent(() => import("@/components/CollapsibleSection.vue")),
);
app.component(
	"Commentaire",
	defineAsyncComponent(() => import("@/components/Commentaire.vue")),
);
app.component(
	"FilterContainer",
	defineAsyncComponent(() => import("@/components/FilterContainer.vue")),
);
app.component(
	"FormContainer",
	defineAsyncComponent(() => import("@/components/FormContainer.vue")),
);
app.component(
	"ModalDeleteConfirm",
	defineAsyncComponent(() => import("@/components/ModalDeleteConfirm.vue")),
);
app.component(
	"ModalFinder",
	defineAsyncComponent(() => import("@/components/ModalFinder.vue")),
);
app.component(
	"NotificationContainer",
	defineAsyncComponent(() => import("@/components/NotificationContainer.vue")),
);
app.component(
	"StatusDisplay",
	defineAsyncComponent(() => import("@/components/StatusDisplay.vue")),
);
app.component(
	"Store",
	defineAsyncComponent(() => import("@/components/Store.vue")),
);
app.component(
	"Tableau",
	defineAsyncComponent(() => import("@/components/Tableau.vue")),
);
app.component(
	"Tags",
	defineAsyncComponent(() => import("@/components/Tags.vue")),
);
app.component(
	"TopButtonEditElement",
	defineAsyncComponent(() => import("@/components/TopButtonEditElement.vue")),
);

app.mount("#app");
