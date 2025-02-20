<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const storeId = route.params.id;

import { useConfigsStore, useStoresStore, useTagsStore, useItemsStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (storeId !== "new") {
		storesStore.storeEdition = {
			loading: true,
		};
		try {
			await storesStore.getStoreById(storeId, ["boxs", "leds"]);
		} catch {
			delete storesStore.stores[storeId];
			addNotification({ message: "store.VStoreNotFound", type: "error", i18n: true });
			router.push("/stores");
			return;
		}
		storesStore.getTagStoreByInterval(storeId, 100, 0, ["tag"]);
		storesStore.storeEdition = {
			loading: false,
			id_store: storesStore.stores[storeId].id_store,
			nom_store: storesStore.stores[storeId].nom_store,
			mqtt_name_store: storesStore.stores[storeId].mqtt_name_store,
			xlength_store: storesStore.stores[storeId].xlength_store,
			ylength_store: storesStore.stores[storeId].ylength_store,
		};
		storesStore.ledEdition = storesStore.leds[storeId];
		storesStore.boxEdition = storesStore.boxs[storeId];
	} else {
		storesStore.storeEdition = {
			loading: false,
		};
		storesStore.ledEdition = {};
		storesStore.boxEdition = {};
	}
	showStoreGrid();
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	storesStore.storeEdition = {
		loading: false,
	};
	storesStore.ledEdition = {};
	storesStore.boxEdition = {};
});

// store
const storeDeleteModalShow = ref(false);
const storeInputTagShow = ref(false);
const tagLoad = ref(false);
const storeSave = async() => {
	if (!checkOutOfGrid()) {
		return;
	}
	try {
		await schemaStore.validate(storesStore.storeEdition, { abortEarly: false });
		
		await storesStore.createStore(storesStore.storeEdition);
		addNotification({ message: "store.VStoreCreated", type: "success", i18n: true });
		await Promise.all(
			storesStore.createLedBulk(storesStore.storeEdition.id_store, storesStore.ledEdition),
			storesStore.createBoxBulk(storesStore.storeEdition.id_store, storesStore.boxEdition));
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		storesStore.storeEdition.loading = false;
		return;
	}
	storeId = storesStore.storeEdition.id_store;
	router.push("/stores/" + storesStore.storeEdition.id_store);
};
const storeUpdate = async() => {
	if (!checkOutOfGrid() || !checkBoxConflict(true)) {
		return;
	}
	try {
		await schemaStore.validate(storesStore.storeEdition, { abortEarly: false });
		// delete all the leds and boxs with the status "delete"
		if (Object.values(storesStore.boxEdition).filter((box) => box.status === "delete").length > 0) {
			let listIdToDelete = [];
			Object.values(storesStore.boxEdition).filter((box) => box.status === "delete").forEach((box) => {
				listIdToDelete.push(box.id_box);
			});
			await storesStore.deleteBoxBulk(storeId, listIdToDelete);
			storesStore.boxEdition = Object.values(storesStore.boxEdition).filter((box) => box.status !== "delete");
		}
		if (Object.values(storesStore.ledEdition).filter((led) => led.status === "delete").length > 0) {
			let listIdToDelete = [];
			Object.values(storesStore.ledEdition).filter((led) => led.status === "delete").forEach((led) => {
				listIdToDelete.push(led.id_led);
			});
			await storesStore.deleteLedBulk(storeId, listIdToDelete);
			storesStore.ledEdition = Object.values(storesStore.ledEdition).filter((led) => led.status !== "delete");
		}
		await storesStore.updateStore(storeId, { ...storesStore.storeEdition });
		addNotification({ message: "store.VStoreUpdated", type: "success", i18n: true });
		if (Object.values(storesStore.boxEdition).filter((box) => box.status === "modified").length > 0) {
			await storesStore.updateBoxBulk(storeId, Object.values(storesStore.boxEdition).filter((box) => box.status === "modified"));
			Object.values(storesStore.boxEdition).forEach((box) => {
				if (box.status === "modified") {
					delete box.status;
				}
			});
		}
		// check if a led as been modified (status = modified or mqtt_led_id != storesStore.leds[storeId][id_led].mqtt_led_id)
		if ((Object.values(storesStore.ledEdition).filter((led) => led.status === "modified" || led.mqtt_led_id !== storesStore.leds[storeId][led.id_led].mqtt_led_id).length) > 0) {
			await storesStore.updateLedBulk(storeId, Object.values(storesStore.ledEdition).filter((led) => led.status === "modified" || led.mqtt_led_id !== storesStore.leds[storeId][led.id_led].mqtt_led_id));
			Object.values(storesStore.ledEdition).forEach((led) => {
				if (led.status === "modified") {
					delete led.status;
				}
			});
		}
		if (Object.values(storesStore.boxEdition).filter((box) => box.status === "new").length > 0) {
			await storesStore.createBoxBulk(storeId, Object.values(storesStore.boxEdition).filter((box) => box.status === "new"));
			Object.values(storesStore.boxEdition).forEach((box) => {
				if (box.status === "new") {
					delete box.status;
				}
			});
		}
		if (Object.values(storesStore.ledEdition).filter((led) => led.status === "new").length > 0) {
			await storesStore.createLedBulk(storeId, Object.values(storesStore.ledEdition).filter((led) => led.status === "new"));
			Object.values(storesStore.ledEdition).forEach((led) => {
				if (led.status === "new") {
					delete led.status;
				}
			});
		}
		// at the end, resync list
		await storesStore.getStoreById(storeId, ["boxs", "leds"]);
		storesStore.ledEdition = storesStore.leds[storeId];
		storesStore.boxEdition = storesStore.boxs[storeId];
		storesStore.storeEdition.loading = false;
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		storesStore.storeEdition.loading = false;
		return;
	}
};
const storeDelete = async() => {
	try {
		await storesStore.deleteStore(storeId);
		addNotification({ message: "store.VStoreDeleted", type: "success", i18n: true });
		router.push("/stores");
	} catch (e) {
		addNotification({ message: "store.VStoreDeleteError", type: "error", i18n: true });
	}
	storeDeleteModalShow.value = false;
};
const showInputAddTag = async() => {
	if (!tagLoad.value) {
		try {
			let offset = 0;
			const limit = 100;
			do {
				await tagsStore.getTagByInterval(limit, offset);
				offset += limit;
			} while (offset < tagsStore.tagsTotalCount);
			tagLoad.value = true;
		} catch (e) {
			console.log(e);
		}
	}
	storeInputTagShow.value = true;
};

const newTags = computed(() => {
	return Object.values(tagsStore.tags).filter((element) => {
		return !storesStore.storeTags[storeId][element.id_tag];
	});
});

// validate the store rezise
function checkOutOfGrid() {
	let errorLed = false;
	Object.keys(storesStore.ledEdition).forEach((led) => {
		if ((storesStore.ledEdition[led].x_led >= storesStore.storeEdition.xlength_store) || (storesStore.ledEdition[led].y_led >= storesStore.storeEdition.ylength_store)) {
			if (storesStore.ledEdition[led]?.status !== "delete") {
				errorLed = true;
			}
		}
	});
	if (errorLed) {
		addNotification({ message: "store.VStoreLedOutOfGrid", type: "error", i18n: true });
		return false;
	}
	let errorBox = false;
	Object.keys(storesStore.boxEdition).forEach((box) => {
		if ((storesStore.boxEdition[box].xend_box > storesStore.storeEdition.xlength_store) || (storesStore.boxEdition[box].yend_box > storesStore.storeEdition.ylength_store)) {
			if (storesStore.boxEdition[box]?.status !== "delete") {
				errorBox = true;
			}
		}
	});
	if (errorBox) {
		addNotification({ message: "store.VStoreBoxOutOfGrid", type: "error", i18n: true });
		return false;
	}
	return true;
}

// check if 2 box are in conflict
function checkBoxConflict(validate = false) {
	Object.values(storesStore.boxEdition).forEach((box) => {
		document.getElementById("BOX" + box.id_box).classList.remove("conflict");
	});
	Object.values(storesStore.boxEdition).forEach((box1) => {
		Object.values(storesStore.boxEdition).forEach((box2) => {
			if (box1.id_box !== box2.id_box) {
				if ((box1.xstart_box < box2.xend_box) &&
				(box1.xend_box > box2.xstart_box) &&
				(box1.ystart_box < box2.yend_box) &&
				(box1.yend_box > box2.ystart_box)) {
					document.getElementById("BOX" + box1.id_box).classList.add("conflict");
					document.getElementById("BOX" + box2.id_box).classList.add("conflict");
				}
			}
		});
	});
	if (validate) {
		var BreakException = {};
		try {
			Object.values(storesStore.boxEdition).forEach((box) => {
				if (document.getElementById("BOX" + box.id_box).classList.contains("conflict")) {
					addNotification({ message: "store.VStoreBoxConflict", type: "error", i18n: true });
					throw BreakException;
				}
			});
			return true;
		} catch (e) {
			return false;
		}
	}
}

// gridOrigin.left = left position of the grid
// gridOrigin.top = top position of the grid
// gridOrigin.cellSizeX = cell size X
// gridOrigin.cellSizeY = cell size Y
// update the grid when the size of the cell change
function showStoreGrid() {
	let gridBox = document.querySelector(".grid");
	gridBox.style.gridTemplateColumns = `repeat(${storesStore.storeEdition.xlength_store}, ${sizeOfCell.value}px)`;
	gridBox.style.gridTemplateRows = `repeat(${storesStore.storeEdition.ylength_store}, ${sizeOfCell.value}px)`;
	gridBox.style.width = `${storesStore.storeEdition.xlength_store * sizeOfCell.value}px`;
	gridBox.style.height = `${storesStore.storeEdition.ylength_store * sizeOfCell.value}px`;
	let gridBoxPos = gridBox.getBoundingClientRect();
	gridOrigin.value.left = gridBoxPos.left + window.scrollX;
	gridOrigin.value.top = gridBoxPos.top + window.scrollY;
	gridOrigin.value.cellSizeX = Math.floor(gridBoxPos.width / storesStore.storeEdition.xlength_store);
	gridOrigin.value.cellSizeY = Math.floor(gridBoxPos.height / storesStore.storeEdition.ylength_store);
}

function getLastLedId() {
	let max = 0;
	Object.keys(storesStore.ledEdition).forEach((led) => {
		if (storesStore.ledEdition[led].id_led > max) {
			max = storesStore.ledEdition[led].id_led;
		}
	});
	return max;
}
function getLastLedMqttId() {
	let max = 0;
	Object.keys(storesStore.ledEdition).forEach((led) => {
		if (storesStore.ledEdition[led].mqtt_led_id > max) {
			max = storesStore.ledEdition[led].mqtt_led_id;
		}
	});
	return max;
}
function addLed() {
	storesStore.pushLed({
		id_led: getLastLedId() + 1,
		x_led: mouseClick.value.X,
		y_led: mouseClick.value.Y,
		mqtt_led_id: getLastLedMqttId() + 1,
		status: "new",
	});
	showMenu.value = false;
}
function getLastBoxId() {
	let max = 0;
	Object.keys(storesStore.boxEdition).forEach((box) => {
		if (storesStore.boxEdition[box].id_box > max) {
			max = storesStore.boxEdition[box].id_box;
		}
	});
	return max;
}
function addBox() {
	storesStore.pushBox({
		id_box: getLastBoxId() + 1,
		xstart_box: mouseClick.value.X,
		ystart_box: mouseClick.value.Y,
		xend_box: mouseClick.value.X + 1,
		yend_box: mouseClick.value.Y + 1,
		status: "new",
	});
	showMenu.value = false;
}

const menuPos = ref({ X: 0, Y: 0 });// en px // position du menu a afficher
const mousePos = ref({ X: 0, Y: 0 });// en cellule // position de la souris dans le tableau
const mouseClick = ref({ X: 0, Y: 0 });// en cellule // position de la souris au moment du click

const selectedElement = ref({
	type: null,
	key: null,
	temp: {},
});

const showMenu = ref(false);
const hasDragElement = ref(false);

const showLedId = ref(false);
const sizeOfCell = ref(20);

// information sur la position du tableau sur la page
const gridOrigin = ref({
	left: 0,
	top: 0,
	cellSizeX: 20,
	cellSizeY: 20,
});

function showNewMenu(event) {
	stopSelecting();
	if (authStore.user?.role_user !== "admin") {
		return;
	}
	showMenu.value = true;
	menuPos.value.X = event.clientX;
	menuPos.value.Y = event.clientY;
	saveMouseClickPos();
}

function showEditMenu(event) {
	showMenu.value = true;
	menuPos.value.X = event.clientX;
	menuPos.value.Y = event.clientY;
}

function hideMenu(event) {
	// if the click is in a div with id 'MenuLedEdit' or 'MenuBoxEdit' don't hide the menu
	if (document.getElementById("MenuLedEdit")?.contains(event.target) || document.getElementById("MenuBoxEdit")?.contains(event.target)) {
		return;
	}
	showMenu.value = false;
	stopSelecting();
}

function saveMouseClickPos() {
	mouseClick.value.X = mousePos.value.X;
	mouseClick.value.Y = mousePos.value.Y;
}

function stopSelecting() {
	document.querySelector(".selectedElement")?.classList.remove("selectedElement");
	document.querySelector(".cursor-move")?.classList.remove("cursor-move");
	document.querySelector(".diagonal-hatch")?.classList.remove("diagonal-hatch");
	if (selectedElement.value.type === "border") {
		document.querySelector(".grid")?.classList.remove("cursor-nw-resize");
		document.querySelector(".grid")?.classList.remove("cursor-ne-resize");
		document.querySelector(".grid")?.classList.remove("cursor-sw-resize");
		document.querySelector(".grid")?.classList.remove("cursor-se-resize");
		document.querySelector(".grid")?.classList.remove("cursor-n-resize");
		document.querySelector(".grid")?.classList.remove("cursor-e-resize");
		document.querySelector(".grid")?.classList.remove("cursor-s-resize");
		document.querySelector(".grid")?.classList.remove("cursor-w-resize");
	}
	selectedElement.value.type = null;
	selectedElement.value.key = null;
	selectedElement.value.temp = {};
}

function selectLed(led, event = null) {
	if (event) {
		showEditMenu(event);
	} // if right click show the menu
	saveMouseClickPos();
	if (selectedElement.value.key === led) {
		return;
	} // if click on the element already selected don't do anything
	stopSelecting(); // unselect if another element is selected
	selectedElement.value.key = led;
	selectedElement.value.type = "led";
	if (led?.status !== "new") {
		led.status = "modified";
	}
	let ledHtml = document.getElementById("LED" + led.id_led);
	ledHtml.classList.add("selectedElement");
}

function selectBox(box, event = null) {
	if (event) {
		showEditMenu(event);
	} // if right click show the menu
	saveMouseClickPos();
	if (selectedElement.value.key === box) {
		return;
	} // if click on the element already selected don't do anything
	stopSelecting(); // unselect if another element is selected
	selectedElement.value.key = box;
	selectedElement.value.type = "box";
	// on stock la valeur de départ de la box
	selectedElement.value.temp.xstart_box = box.xstart_box;
	selectedElement.value.temp.ystart_box = box.ystart_box;
	selectedElement.value.temp.xend_box = box.xend_box;
	selectedElement.value.temp.yend_box = box.yend_box;
	selectedElement.value.status = "modified";
	if (box?.status !== "new") {
		box.status = "modified";
	}
	let boxHtml = document.getElementById("BOX" + box.id_box);
	boxHtml.classList.add("selectedElement");
	boxHtml.classList.add("diagonal-hatch");
}

function selectBorder(border, direction) {
	if (selectedElement.value.key === border) {
		stopSelecting(); // unselect if click on the selectedElement element
		return;
	}
	stopSelecting(); // unselect if click on another element
	selectedElement.value.key = border;
	selectedElement.value.type = "border";
	// on stock la valeur de départ de la box lier au border
	selectedElement.value.temp.xstart_box = border.xstart_box;
	selectedElement.value.temp.ystart_box = border.ystart_box;
	selectedElement.value.temp.xend_box = border.xend_box;
	selectedElement.value.temp.yend_box = border.yend_box;
	selectedElement.value.temp.direction = direction;
	let boxHtml = document.getElementById("BOX" + border.id_box);
	boxHtml.classList.add("selectedElement");
	boxHtml.classList.add("diagonal-hatch");
}

function startDragging(element, type, direction = null) {
	if (authStore.user?.role_user !== "admin") {
		return;
	}
	if (hasDragElement.value) {
		return;
	}
	saveMouseClickPos();
	// if the element is not the selected element, change the selected element
	if ((selectedElement.value.key !== element) || (selectedElement.value.type !== type)) {
		if (type === "led") {
			selectLed(element);
			document.querySelector(".grid")?.classList.add("cursor-move");
		} else if (type === "box") {
			selectBox(element);
			document.querySelector(".grid")?.classList.add("cursor-move");
		} else if (type === "border") {
			selectBorder(element, direction);
			document.querySelector(".grid")?.classList.add("cursor-" + direction);
		}
	}
	hasDragElement.value = true;
	showMenu.value = false;
}

function stopDragging() {
	if (!hasDragElement.value) {
		return;
	}
	hasDragElement.value = false;
	if (selectedElement.value.type === "led") {
		stopSelecting();
	}
}

function moveMouse(event) {
	let gridBoxPos = document.querySelector(".grid").getBoundingClientRect();
	gridOrigin.value.left = gridBoxPos.left + window.scrollX;
	gridOrigin.value.top = gridBoxPos.top + window.scrollY;
	gridOrigin.value.cellSizeX = Math.floor(gridBoxPos.width / storesStore.storeEdition.xlength_store);
	gridOrigin.value.cellSizeY = Math.floor(gridBoxPos.height / storesStore.storeEdition.ylength_store);
	let x = Math.floor((event.clientX - gridOrigin.value.left + window.scrollX) / gridOrigin.value.cellSizeX);
	let y = storesStore.storeEdition.ylength_store - Math.floor((event.clientY - gridOrigin.value.top + window.scrollY) / gridOrigin.value.cellSizeY) - 1; // reverse Y axis
	// if a value is out of the grid, don't update the mousePos
	if ((x < 0) || (y < 0) || (x >= storesStore.storeEdition.xlength_store) || (y >= storesStore.storeEdition.ylength_store)) {
		return;
	}
	mousePos.value.X = x;
	mousePos.value.Y = y;
	if (hasDragElement.value) {
		if (selectedElement.value.type === "led") {
			selectedElement.value.key.x_led = x;
			selectedElement.value.key.y_led = y;
		} else if (selectedElement.value.type === "box") {
			let dx = x - mouseClick.value.X;
			let dy = y - mouseClick.value.Y;
			if ((selectedElement.value.temp.xstart_box + dx < 0) ||
				(selectedElement.value.temp.ystart_box + dy < 0) ||
				(selectedElement.value.temp.xend_box + dx > storesStore.storeEdition.xlength_store) ||
				(selectedElement.value.temp.yend_box + dy > storesStore.storeEdition.ylength_store)) {
				return;
			}
			selectedElement.value.key.xstart_box = selectedElement.value.temp.xstart_box + dx;
			selectedElement.value.key.ystart_box = selectedElement.value.temp.ystart_box + dy;
			selectedElement.value.key.xend_box = selectedElement.value.temp.xend_box + dx;
			selectedElement.value.key.yend_box = selectedElement.value.temp.yend_box + dy;
			checkBoxConflict();
		} else if (selectedElement.value.type === "border") {
			if (selectedElement.value.temp.direction === "ne" || selectedElement.value.temp.direction === "se" || selectedElement.value.temp.direction === "e") {
				if (mousePos.value.X >= selectedElement.value.temp.xstart_box) {
					selectedElement.value.key.xend_box = selectedElement.value.temp.xend_box + (mousePos.value.X - mouseClick.value.X);
				}
			}
			if (selectedElement.value.temp.direction === "nw" || selectedElement.value.temp.direction === "ne" || selectedElement.value.temp.direction === "n") {
				if (mousePos.value.Y >= selectedElement.value.temp.ystart_box) {
					selectedElement.value.key.yend_box = selectedElement.value.temp.yend_box + (mousePos.value.Y - mouseClick.value.Y);
				}
			}
			if (selectedElement.value.temp.direction === "nw" || selectedElement.value.temp.direction === "sw" || selectedElement.value.temp.direction === "w") {
				if (mousePos.value.X < selectedElement.value.temp.xend_box) {
					selectedElement.value.key.xstart_box = selectedElement.value.temp.xstart_box + (mousePos.value.X - mouseClick.value.X);
				}
			}
			if (selectedElement.value.temp.direction === "sw" || selectedElement.value.temp.direction === "se" || selectedElement.value.temp.direction === "s") {
				if (mousePos.value.Y < selectedElement.value.temp.yend_box) {
					selectedElement.value.key.ystart_box = selectedElement.value.temp.ystart_box + (mousePos.value.Y - mouseClick.value.Y);
				}
			}
			checkBoxConflict();
		}
	}
}

function deleteElement() {
	if (selectedElement.value.type === "led") {
		Object.keys(storesStore.ledEdition).forEach((index) => {
			if (storesStore.ledEdition[index] === selectedElement.value.key) {
				if (storesStore.ledEdition[index].status === "new") {
					delete storesStore.ledEdition[index];
				} else {
					storesStore.ledEdition[index].status = "delete";
				}
			}
		});
	} else if (selectedElement.value.type === "box") {
		Object.keys(storesStore.boxEdition).forEach((index) => {
			if (storesStore.boxEdition[index] === selectedElement.value.key) {
				if (storesStore.boxEdition[index].status === "new") {
					delete storesStore.boxEdition[index];
				} else {
					storesStore.boxEdition[index].status = "delete";
				}
			}
		});
	}
	showMenu.value = false;
	stopSelecting();
}

// stop drag in all this page
document.addEventListener("mouseup", stopDragging);
// hide the menu if click outside
document.addEventListener("click", hideMenu);

const sortedTags = computed(() => {
	return Object.keys(storesStore.storeTags[storeId] || {})
		.sort((a, b) => tagsStore.tags[b].poids_tag - tagsStore.tags[a].poids_tag);
});

const schemaStore = Yup.object().shape({
	nom_store: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("store.VStoreNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("store.VStoreNameRequired")),
	mqtt_name_store: Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("store.VStoreMQTTNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
		.required(t("store.VStoreMQTTNameRequired")),
	xlength_store: Yup.number()
		.min(1, t("store.VStoreXLengthMin"))
		.typeError(t("store.VStoreXLengthType"))
		.required(t("store.VStoreXLengthRequired")),
	ylength_store: Yup.number()
		.min(1, t("store.VStoreYLengthMin"))
		.typeError(t("store.VStoreYLengthType"))
		.required(t("store.VStoreYLengthRequired")),
});

function isNumber(value) {
	return typeof value === "number";
}

const toggleLed = async(ledId) => {
	try {
		await storesStore.showLedById(storeId, ledId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
	} catch (e) {
		addNotification({ message: "store.VStoreToggleError", type: "error", i18n: true });
	}
};
const toggleBoxLed = async(boxId) => {
	try {
		await storesStore.showBoxById(storeId, boxId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
	} catch (e) {
		addNotification({ message: "store.VStoreToggleError", type: "error", i18n: true });
	}
};
</script>
<style>
.grid {
	position: relative;
	display: grid;
	grid-template-columns: repeat(20, 20px);
	grid-template-rows: repeat(20, 20px);
	background-color: #ddd;
	width: 400px;
	height: 400px;
}

.cell {
	background-color: white;
	border: 1px solid #ccc;
}

.led {
	width: 10px;
	height: 10px;
	background-color: rgb(255, 208, 0);
	border-radius: 50%;
	position: absolute;
	transform: translate(-50%, -50%);
}

.box {
	border: 2px solid rgb(9, 255, 0);
	position: absolute;
	overflow: hidden;
}

.box.conflict {
	border-color: red;
}

.selectedElement {
	background-color: rgb(0, 174, 255);
}

.context-menu {
	position: fixed;
	background-color: white;
	border: 1px solid #ccc;
	padding: 5px;
	z-index: 100;
}

.diagonal-hatch {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background-image: repeating-linear-gradient(45deg,
			rgba(0, 255, 0, 0.1),
			rgba(0, 255, 0, 0.1) 10px,
			transparent 10px,
			transparent 20px);
}

.resizer {
	background-color: #2c3e50;
	position: absolute;
	z-index: 11;
}

.resizer.corner {
	width: 10px;
	height: 10px;
}

.resizer.edge {
	background-color: transparent;
}

.resizer.n,
.resizer.s {
	height: 10px;
	left: 5px;
	right: 5px;
}

.resizer.e,
.resizer.w {
	width: 10px;
	top: 5px;
	bottom: 5px;
}

.resizer.n {
	top: -5px;
}

.resizer.e {
	right: -5px;
}

.resizer.s {
	bottom: -5px;
}

.resizer.w {
	left: -5px;
}

.resizer.nw {
	top: -5px;
	left: -5px;
}

.resizer.ne {
	top: -5px;
	right: -5px;
}

.resizer.sw {
	bottom: -5px;
	left: -5px;
}

.resizer.se {
	bottom: -5px;
	right: -5px;
}

.no-select {
	user-select: none;
	/* Standard */
	-webkit-user-select: none;
	/* Safari */
	-moz-user-select: none;
	/* Firefox */
	-ms-user-select: none;
	/* Internet Explorer/Edge */
}
</style>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('store.VStoreTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="storeSave" v-if="storeId == 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="storesStore.storeEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('store.VStoreAdd') }}
			</button>
			<button type="button" @click="storeUpdate"
				v-else-if="storeId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="storesStore.storeEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('store.VStoreUpdate') }}
			</button>
			<button type="button" @click="storeDeleteOpenModal"
				v-if="storeId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('store.VStoreDelete') }}
			</button>
			<RouterLink to="/stores"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('store.VStoreBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="storesStore.stores[storeId] || storeId == 'new'">
		<div class="mb-6 flex justify-between">
			<Form :validation-schema="schemaStore" v-slot="{ errors }" @submit.prevent="">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('store.VStoreName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_store" type="text"
									v-model="storesStore.storeEdition.nom_store"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_store }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_store || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('store.VStoreMQTTName') }}:</td>
							<td class="flex flex-col">
								<Field name="mqtt_name_store" type="text"
									v-model="storesStore.storeEdition.mqtt_name_store"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.mqtt_name_store }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.mqtt_name_store || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('store.VStoreXLength') }}:</td>
							<td class="flex flex-col">
								<Field name="xlength_store" type="number"
									v-model="storesStore.storeEdition.xlength_store" @change="showStoreGrid" @keyup="showStoreGrid"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.xlength_store }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.xlength_store || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('store.VStoreYLength') }}:</td>
							<td class="flex flex-col">
								<Field name="ylength_store" type="number"
									v-model="storesStore.storeEdition.ylength_store" @change="showStoreGrid" @keyup="showStoreGrid"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.ylength_store }" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.ylength_store || ' ' }}</span>
							</td>
						</tr>
					</tbody>
				</table>
				<div class="flex space-x-4">
					<span>{{ $t('store.VStoreCellSize') }}</span>
					<input type="range" v-model="sizeOfCell" min="10" max="50" step="10" @input="showStoreGrid" />
				</div>
				<div class="flex space-x-4">
					<span>{{ $t('store.VStoreShowLedId') }}</span>
					<input type="checkbox" v-model="showLedId" />
				</div>
			</Form>
			<div class="w-96 h-96 bg-gray-200 px-2 py-2 rounded">
				<span v-for="(value, key) in sortedTags" :key="key"
					class="bg-gray-300 p-1 rounded mr-2 mb-2 whitespace-pre">
					{{ tagsStore.tags[value].nom_tag }} ({{ tagsStore.tags[value].poids_tag }})
					<span @click="storesStore.deleteTagStore(storeId, value)"
						class="text-red-500 cursor-pointer hover:text-red-600">
						<font-awesome-icon icon="fa-solid fa-times" />
					</span>
				</span>
				<span v-if="!storeInputTagShow" class="bg-gray-300 p-1 rounded mr-2 mb-2">
					<span @click="showInputAddTag"
						class="text-green-500 cursor-pointer hover:text-green-600">
						<font-awesome-icon icon="fa-solid fa-plus" />
					</span>
				</span>
			</div>
		</div>
		<div class="grid" @contextmenu.prevent="showNewMenu" @mousemove="moveMouse"
			:class="isNumber(storesStore.storeEdition.xlength_store) && isNumber(storesStore.storeEdition.ylength_store) ? '' : 'cursor-not-allowed'">
			<template v-if="isNumber(storesStore.storeEdition.xlength_store) && isNumber(storesStore.storeEdition.ylength_store)">
				<div v-for="i in storesStore.storeEdition.xlength_store * storesStore.storeEdition.ylength_store"
					:key="i" class="cell"></div>
				<div v-for="led in storesStore.ledEdition" :key="led.id_led" class="led"
					:id="'LED' + led.id_led"
					:style="{
						left: (led.x_led * gridOrigin.cellSizeX) + gridOrigin.cellSizeX / 2 + 'px',
						top: (((storesStore.storeEdition.ylength_store - 1) - led.y_led) * gridOrigin.cellSizeY) + gridOrigin.cellSizeY / 2 + 'px',
						zIndex: 20}"
					:class="{ 'hidden': led?.status == 'delete' }"
					@mousedown.left="startDragging(led, 'led')"
					@contextmenu.prevent="(event) => selectLed(led, event)"
					@contextmenu.stop>
				</div>
				<template v-if="showLedId">
					<div v-for="led in storesStore.ledEdition" :key="led.id_led" class="no-select"
						:style="{
							left: (led.x_led * gridOrigin.cellSizeX) + gridOrigin.cellSizeX / 2 + 10 + 'px',
							top: (((storesStore.storeEdition.ylength_store - 1) - led.y_led) * gridOrigin.cellSizeY) + gridOrigin.cellSizeY / 2 + 8 + 'px',
							zIndex: 20,
							position: 'absolute'}"
						:class="{ 'hidden': led?.status == 'delete' }">
						{{ led.mqtt_led_id }}
					</div>
				</template>
				<div v-for="box in storesStore.boxEdition" :key="box.id_box" class="box" :id="'BOX' + box.id_box"
					:style="{
						left: (box.xstart_box * gridOrigin.cellSizeX) + 'px',
						top: (((storesStore.storeEdition.ylength_store) - box.yend_box) * gridOrigin.cellSizeY) + 'px',
						width: ((box.xend_box - box.xstart_box) * (gridOrigin.cellSizeX)) + 'px',
						height: ((box.yend_box - box.ystart_box) * (gridOrigin.cellSizeY)) + 'px',
						zIndex: 10}"
					:class="{ 'hidden': box?.status == 'delete' }"
					@mousedown.left="startDragging(box, 'box')"
					@contextmenu.prevent="(event) => selectBox(box, event)"
					@contextmenu.stop>
					<div v-if="authStore.user?.role_user === 'admin' && !showMenu && (selectedElement.type == null || selectedElement.key == box) ">
						<div class="resizer corner nw cursor-nw-resize" @mousedown.left="startDragging(box, 'border', 'nw')"
							@contextmenu.stop>
						</div>
						<div class="resizer corner ne cursor-ne-resize" @mousedown.left="startDragging(box, 'border', 'ne')"
							@contextmenu.stop>
						</div>
						<div class="resizer corner sw cursor-sw-resize" @mousedown.left="startDragging(box, 'border', 'sw')"
							@contextmenu.stop>
						</div>
						<div class="resizer corner se cursor-se-resize" @mousedown.left="startDragging(box, 'border', 'se')"
							@contextmenu.stop>
						</div>
						<div class="resizer edge n cursor-n-resize" @mousedown.left="startDragging(box, 'border', 'n')"
							@contextmenu.stop>
						</div>
						<div class="resizer edge e cursor-e-resize" @mousedown.left="startDragging(box, 'border', 'e')"
							@contextmenu.stop>
						</div>
						<div class="resizer edge s cursor-s-resize" @mousedown.left="startDragging(box, 'border', 's')"
							@contextmenu.stop>
						</div>
						<div class="resizer edge w cursor-w-resize" @mousedown.left="startDragging(box, 'border', 'w')"
							@contextmenu.stop>
						</div>	
					</div>
					<div v-else>
						<div class="resizer corner nw"></div>
						<div class="resizer corner ne"></div>
						<div class="resizer corner sw"></div>
						<div class="resizer corner se"></div>
						<div class="resizer edge n"></div>
						<div class="resizer edge e"></div>
						<div class="resizer edge s"></div>
						<div class="resizer edge w"></div>	
					</div>	
				</div>
			</template>
			<template v-else>
				<!-- TODO : add loading animation -->
			</template>
		</div>
	</div>
	<template v-if="isNumber(storesStore.storeEdition.xlength_store) && isNumber(storesStore.storeEdition.ylength_store)">
		<div v-if="showMenu && !selectedElement.type" class="context-menu"
			:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }">
			<div class="flex flex-col">
				<button @click="addLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreAddLed') }}
				</button>
				<button @click="addBox" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreAddBox') }}
				</button>
			</div>
		</div>
		<div v-if="showMenu && selectedElement.type == 'led'" class="context-menu"
			:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }" id="MenuLedEdit">
			<div class="flex flex-col">
				<button v-if="authStore.user?.role_user === 'admin'" @click="deleteElement" class="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600">
					{{ $t('store.VStoreDeleteLed') }}
				</button>
				<button @click="toggleLed(selectedElement.key.id_led)" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreToggleLed') }}
				</button>
				<div class="flex space-x-4">
					<span>{{ $t('store.VStoreMqttLedId') }}</span>
					<input type="number" v-model="storesStore.ledEdition[selectedElement.key.id_led].mqtt_led_id" class="w-16" :disabled="authStore.user?.role_user !== 'admin'" />
				</div>
				<!-- TODO : add color weel and select animation and light duration -->
			</div>
		</div>
		<div v-if="showMenu && selectedElement.type == 'box'" class="context-menu"
			:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }" id="MenuBoxEdit">
			<div class="flex flex-col">
				<button v-if="authStore.user?.role_user === 'admin'" @click="deleteElement" class="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600">
					{{ $t('store.VStoreDeleteBox') }}
				</button>
				<button @click="showBoxContent" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreShowBoxContent') }}
				</button>
				<button @click="toggleBoxLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreToggleBoxLed') }}
				</button>
				<button v-if="authStore.user?.role_user === 'admin'" @click="addLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
					{{ $t('store.VStoreAddLed') }}
				</button>
			</div>
		</div>
		<div>
			<span>X: </span>
			<strong>{{ mousePos.X }}</strong>
			<span>Y: </span>
			<strong>{{ mousePos.Y }}</strong>
		</div>
	</template>

	<div v-if="storeInputTagShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
		@click="storeInputTagShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<div class="flex justify-between items-center border-b pb-3">
				<h2 class="text-2xl font-semibold">{{ $t('store.VStoreAddTag') }}</h2>
				<button type="button" @click="storeInputTagShow = false"
					class="text-gray-500 hover:text-gray-700">&times;</button>
			</div>

			<div class="flex flex-wrap">
				<template v-for="(tag, key) in newTags" :key="key">
					<div class="bg-gray-200 p-1 rounded mr-2 mb-2 cursor-pointer"
						@click="storesStore.createTagStore(storeId, { id_tag: tag.id_tag })">
						{{ tag.nom_tag }} ({{ tag.poids_tag }})
					</div>
				</template>
			</div>
		</div>
	</div>

	<div v-if="storeDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="storeDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('store.VStoreDeleteTitle') }}</h2>
			<p>{{ $t('store.VStoreDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="storeDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('store.VStoreDeleteConfirm') }}
				</button>
				<button type="button" @click="storeDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('store.VStoreDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>
