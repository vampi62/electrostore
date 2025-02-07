<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const storeId = route.params.id;

import { useStoresStore, useTagsStore, useItemsStore, useAuthStore } from "@/stores";
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchData() {
	if (storeId !== "new") {
		storesStore.storeEdition = {
			loading: false,
		};
		try {
			await storesStore.getStoreById(storeId);
		} catch {
			delete storesStore.stores[storeId];
			addNotification({ message: "store.VStoreNotFound", type: "error", i18n: true });
			router.push("/stores");
			return;
		}
		storesStore.storeEdition = {
			loading: false,
			nom_store: storesStore.stores[storeId].nom_store,
			mqtt_name_store: storesStore.stores[storeId].mqtt_name_store,
			xlength_store: storesStore.stores[storeId].xlength_store,
			ylength_store: storesStore.stores[storeId].ylength_store,
		};
	} else {
		storesStore.storeEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	storesStore.storeEdition = {
		loading: false,
	};
});
const storeInfo = ref({
	DBId: 0,
	DBLenX: 20,
	DBLenY: 20,
	DBName: "Name",
	DBMQTTName: "MQTTName",
	LenX: 20,
	LenY: 20,
	Name: "Name",
	MQTTName: "MQTTName",
});
const ledInfo = ref([]);
let nextLedId = 0;
const boxInfo = ref([]);
let nextBoxId = 0;

function addLed() {
	ledInfo.value.push({
		id: nextLedId++,
		DBId: 0,
		DBX: 0,
		DBY: 0,
		DBMQTTId: 0,
		X: mouseClick.value.X,
		Y: mouseClick.value.Y,
		MQTTId: 0,
		Light: {
			R: 0,
			G: 0,
			B: 0,
			mode: 0,
			state: false,
		},
	});
	showMenu.value = false;
}

function addBox() {
	boxInfo.value.push({
		id: nextBoxId++,
		DBId: 0,
		DBXMin: 0,
		DBYMin: 0,
		DBXMax: 0,
		DBYMax: 0,
		XMin: mouseClick.value.X,
		YMin: mouseClick.value.Y,
		XMax: mouseClick.value.X + 1,
		YMax: mouseClick.value.Y + 1,
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
		document.querySelector(".grid")?.classList.remove("cursor-nw");
		document.querySelector(".grid")?.classList.remove("cursor-ne");
		document.querySelector(".grid")?.classList.remove("cursor-sw");
		document.querySelector(".grid")?.classList.remove("cursor-se");
		document.querySelector(".grid")?.classList.remove("cursor-n");
		document.querySelector(".grid")?.classList.remove("cursor-e");
		document.querySelector(".grid")?.classList.remove("cursor-s");
		document.querySelector(".grid")?.classList.remove("cursor-w");
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
	let ledHtml = document.getElementById("LED" + led.id);
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
	selectedElement.value.temp.XMin = box.XMin;
	selectedElement.value.temp.YMin = box.YMin;
	selectedElement.value.temp.XMax = box.XMax;
	selectedElement.value.temp.YMax = box.YMax;
	let boxHtml = document.getElementById("BOX" + box.id);
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
	selectedElement.value.temp.XMin = border.XMin;
	selectedElement.value.temp.YMin = border.YMin;
	selectedElement.value.temp.XMax = border.XMax;
	selectedElement.value.temp.YMax = border.YMax;
	selectedElement.value.temp.direction = direction;
	let boxHtml = document.getElementById("BOX" + border.id);
	boxHtml.classList.add("selectedElement");
	boxHtml.classList.add("diagonal-hatch");
}

function startDragging(element, type, direction = null) {
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
	gridOrigin.value.cellSizeX = Math.floor(gridBoxPos.width / storeInfo.value.DBLenX);
	gridOrigin.value.cellSizeY = Math.floor(gridBoxPos.height / storeInfo.value.DBLenY);
	let x = Math.floor((event.clientX - gridOrigin.value.left + window.scrollX) / gridOrigin.value.cellSizeX);
	let y = storeInfo.value.DBLenY - Math.floor((event.clientY - gridOrigin.value.top + window.scrollY) / gridOrigin.value.cellSizeY) - 1; // reverse Y axis
	// if a value is out of the grid, don't update the mousePos
	if ((x < 0) || (y < 0) || (x >= storeInfo.value.DBLenX) || (y >= storeInfo.value.DBLenY)) {
		return;
	}
	mousePos.value.X = x;
	mousePos.value.Y = y;
	if (hasDragElement.value) {
		if (selectedElement.value.type === "led") {
			selectedElement.value.key.X = x;
			selectedElement.value.key.Y = y;
		} else if (selectedElement.value.type === "box") {
			let dx = x - mouseClick.value.X;
			let dy = y - mouseClick.value.Y;
			if ((selectedElement.value.temp.XMin + dx < 0) ||
				(selectedElement.value.temp.YMin + dy < 0) ||
				(selectedElement.value.temp.XMax + dx > storeInfo.value.DBLenX) ||
				(selectedElement.value.temp.YMax + dy > storeInfo.value.DBLenY)) {
				return;
			}
			selectedElement.value.key.XMin = selectedElement.value.temp.XMin + dx;
			selectedElement.value.key.YMin = selectedElement.value.temp.YMin + dy;
			selectedElement.value.key.XMax = selectedElement.value.temp.XMax + dx;
			selectedElement.value.key.YMax = selectedElement.value.temp.YMax + dy;
			checkConflict();
		} else if (selectedElement.value.type === "border") {
			if (selectedElement.value.temp.direction === "ne" || selectedElement.value.temp.direction === "se" || selectedElement.value.temp.direction === "e") {
				selectedElement.value.key.XMax = selectedElement.value.temp.XMax + (mousePos.value.X - mouseClick.value.X);
			}
			if (selectedElement.value.temp.direction === "nw" || selectedElement.value.temp.direction === "ne" || selectedElement.value.temp.direction === "n") {
				selectedElement.value.key.YMax = selectedElement.value.temp.YMax + (mousePos.value.Y - mouseClick.value.Y);
			}
			if (selectedElement.value.temp.direction === "nw" || selectedElement.value.temp.direction === "sw" || selectedElement.value.temp.direction === "w") {
				selectedElement.value.key.XMin = selectedElement.value.temp.XMin + (mousePos.value.X - mouseClick.value.X);
			}
			if (selectedElement.value.temp.direction === "sw" || selectedElement.value.temp.direction === "se" || selectedElement.value.temp.direction === "s") {
				selectedElement.value.key.YMin = selectedElement.value.temp.YMin + (mousePos.value.Y - mouseClick.value.Y);
			}
			checkConflict();
		}
	}
}

// check if 2 box are in conflict
function checkConflict() {
	for (let i = 0; i < boxInfo.value.length; i++) {
		document.getElementById("BOX" + boxInfo.value[i].id).classList.remove("conflict");
	}
	for (let i = 0; i < boxInfo.value.length; i++) {
		for (let j = i + 1; j < boxInfo.value.length; j++) {
			if ((boxInfo.value[i].XMin < boxInfo.value[j].XMax) &&
				(boxInfo.value[i].XMax > boxInfo.value[j].XMin) &&
				(boxInfo.value[i].YMin < boxInfo.value[j].YMax) &&
				(boxInfo.value[i].YMax > boxInfo.value[j].YMin)) {
				document.getElementById("BOX" + boxInfo.value[i].id).classList.add("conflict");
				document.getElementById("BOX" + boxInfo.value[j].id).classList.add("conflict");
			}
		}
	}
}

function deleteElement() {
	if (selectedElement.value.type === "led") {
		for (let i = 0; i < ledInfo.value.length; i++) {
			if (ledInfo.value[i] === selectedElement.value.key) {
				ledInfo.value.splice(i, 1);
				break;
			}
		}
	} else if (selectedElement.value.type === "box") {
		for (let i = 0; i < boxInfo.value.length; i++) {
			if (boxInfo.value[i] === selectedElement.value.key) {
				boxInfo.value.splice(i, 1);
				break;
			}
		}
	}
	showMenu.value = false;
	stopSelecting();
}

function debugData() {
	console.log(selectedElement.value.key);
}

function changeStoreSize() {
	//check if a led or a box is out of the grid
	let errorLed = false;
	for (let i = 0; i < ledInfo.value.length; i++) {
		if ((ledInfo.value[i].X >= storeInfo.value.LenX) || (ledInfo.value[i].Y >= storeInfo.value.LenY)) {
			errorLed = true;
			break;
		}
	}
	if (errorLed) {
		alert("Une ou plusieurs LED sont en dehors du tableau");
		return;
	}
	let errorBox = false;
	for (let i = 0; i < boxInfo.value.length; i++) {
		if ((boxInfo.value[i].XMax > storeInfo.value.LenX) || (boxInfo.value[i].YMax > storeInfo.value.LenY)) {
			errorBox = true;
			break;
		}
	}
	if (errorBox) {
		alert("Une ou plusieurs BOX sont en dehors du tableau");
		return;
	}
	storeInfo.value.DBLenX = storeInfo.value.LenX;
	storeInfo.value.DBLenY = storeInfo.value.LenY;
	showStoreGrid(); // update the grid with the new size
}

// show the grid with the new size
function showStoreGrid() {
	let gridBox = document.querySelector(".grid");
	gridBox.style.gridTemplateColumns = `repeat(${storeInfo.value.LenX}, ${sizeOfCell.value}px)`;
	gridBox.style.gridTemplateRows = `repeat(${storeInfo.value.LenY}, ${sizeOfCell.value}px)`;
	gridBox.style.width = `${storeInfo.value.LenX * sizeOfCell.value}px`;
	gridBox.style.height = `${storeInfo.value.LenY * sizeOfCell.value}px`;
	let gridBoxPos = gridBox.getBoundingClientRect();
	gridOrigin.value.left = gridBoxPos.left + window.scrollX;
	gridOrigin.value.top = gridBoxPos.top + window.scrollY;
	gridOrigin.value.cellSizeX = Math.floor(gridBoxPos.width / storeInfo.value.DBLenX);
	gridOrigin.value.cellSizeY = Math.floor(gridBoxPos.height / storeInfo.value.DBLenY);
}

// stop drag in all this page
document.addEventListener("mouseup", stopDragging);
// hide the menu if click outside
document.addEventListener("click", hideMenu);

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

.cursor-nw {
	cursor: nw-resize;
}

.cursor-ne {
	cursor: ne-resize;
}

.cursor-sw {
	cursor: sw-resize;
}

.cursor-se {
	cursor: se-resize;
}

.cursor-n {
	cursor: n-resize;
}

.cursor-e {
	cursor: e-resize;
}

.cursor-s {
	cursor: s-resize;
}

.cursor-w {
	cursor: w-resize;
}

.cursor-move {
	cursor: move;
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
	<div>
		<input type="text" v-model="storesStore.storeEdition.nom_store"><br>
		<input type="text" v-model="storesStore.storeEdition.mqtt_name_store"><br>
		<span>X: </span>
		<input type="number" v-model="storesStore.storeEdition.xlength_store"><br>
		<span>Y: </span>
		<input type="number" v-model="storesStore.storeEdition.ylength_store"><br>
		<span>Cell size: </span>
		<input type="range" v-model="sizeOfCell" min="10" max="50" step="10"><br>
		<button @click="changeStoreSize">changer la taille du tableau</button><br>
		<br>
		<br>
		<span>show led id</span>
		<input type="checkbox" v-model="showLedId">
	</div>
	<!--
	gridOrigin.left = left position of the grid
	gridOrigin.top = top position of the grid
	gridOrigin.cellSizeX = cell size X
	gridOrigin.cellSizeY = cell size Y
	-->
	<div class="grid" @contextmenu.prevent="showNewMenu" @mousemove="moveMouse">
		<div v-for="i in storeInfo.DBLenX * storeInfo.DBLenY" :key="i" class="cell"></div>
		<div v-for="led in ledInfo" :key="led.id" class="led" :id="'LED' + led.id" :style="{
			left: (led.X * gridOrigin.cellSizeX) + gridOrigin.cellSizeX / 2 + 'px',
			top: (((storeInfo.DBLenY - 1) - led.Y) * gridOrigin.cellSizeY) + gridOrigin.cellSizeY / 2 + 'px',
			zIndex: 20
		}" @mousedown.left="startDragging(led, 'led')" @contextmenu.prevent="(event) => selectLed(led, event)"
			@contextmenu.stop>
		</div>
		<template v-if="showLedId">
			<div v-for="led in ledInfo" :key="led.id" class="no-select" :style="{
				left: (led.X * gridOrigin.cellSizeX) + gridOrigin.cellSizeX / 2 + 10 + 'px',
				top: (((storeInfo.DBLenY - 1) - led.Y) * gridOrigin.cellSizeY) + gridOrigin.cellSizeY / 2 + 8 + 'px',
				zIndex: 20,
				position: 'absolute'
			}">
				{{ led.id }}
			</div>
		</template>
		<div v-for="box in boxInfo" :key="box.id" class="box" :id="'BOX' + box.id" :style="{
			left: (box.XMin * gridOrigin.cellSizeX) + 'px',
			top: (((storeInfo.DBLenY) - box.YMax) * gridOrigin.cellSizeY) + 'px',
			width: ((box.XMax - box.XMin) * (gridOrigin.cellSizeX)) + 'px',
			height: ((box.YMax - box.YMin) * (gridOrigin.cellSizeY)) + 'px',
			zIndex: 10
		}" @mousedown.left="startDragging(box, 'box')" @contextmenu.prevent="(event) => selectBox(box, event)"
			@contextmenu.stop>
			<div v-if="!showMenu && (selectedElement.type == null || selectedElement.key == box)">
				<div class="resizer corner nw cursor-nw" @mousedown.left="startDragging(box, 'border', 'nw')"
					@contextmenu.stop>
				</div>
				<div class="resizer corner ne cursor-ne" @mousedown.left="startDragging(box, 'border', 'ne')"
					@contextmenu.stop>
				</div>
				<div class="resizer corner sw cursor-sw" @mousedown.left="startDragging(box, 'border', 'sw')"
					@contextmenu.stop>
				</div>
				<div class="resizer corner se cursor-se" @mousedown.left="startDragging(box, 'border', 'se')"
					@contextmenu.stop>
				</div>
				<div class="resizer edge n cursor-n" @mousedown.left="startDragging(box, 'border', 'n')"
					@contextmenu.stop>
				</div>
				<div class="resizer edge e cursor-e" @mousedown.left="startDragging(box, 'border', 'e')"
					@contextmenu.stop>
				</div>
				<div class="resizer edge s cursor-s" @mousedown.left="startDragging(box, 'border', 's')"
					@contextmenu.stop>
				</div>
				<div class="resizer edge w cursor-w" @mousedown.left="startDragging(box, 'border', 'w')"
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
	</div>
	<div v-if="showMenu && !selectedElement.type" class="context-menu"
		:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }">
		<button @click="addLed">Ajouter une LED</button><br>
		<button @click="addBox">Ajouter une BOX</button>
	</div>
	<div v-if="showMenu && selectedElement.type == 'led'" class="context-menu"
		:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }" id="MenuLedEdit">
		<button @click="deleteElement">Supprimer la led</button><br>
		<button>change ID led</button><br>
		<button>toggle led</button>
	</div>
	<div v-if="showMenu && selectedElement.type == 'box'" class="context-menu"
		:style="{ left: menuPos.X + 'px', top: menuPos.Y + 'px' }" id="MenuBoxEdit">
		<button @click="deleteElement">Supprimer la box</button><br>
		<button @click="debugData">voir le contenue</button><br>
		<button @click="addLed">Ajouter une LED</button>
	</div>
	<div>
		<span>X: </span>
		<strong>{{ mousePos.X }}</strong>
		<span>Y: </span>
		<strong>{{ mousePos.Y }}</strong>
	</div>
</template>
