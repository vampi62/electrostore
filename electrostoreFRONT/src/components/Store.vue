<template>
	<div class="grid relative bg-color-200 gap-0"
		@contextmenu.prevent="(event) => showMenuModal(event, true)" @mousemove="moveMouse"
		:style="getGridCss"
	>
		<template v-if="isNumber(storeData.xlength_store) && isNumber(storeData.ylength_store)">
			<div v-for="i in storeData.xlength_store * storeData.ylength_store"
				:key="i" class="border bg-white">
			</div>
			<div v-for="led in ledEdition" :key="led.id_led" class="absolute z-[20] w-[10px] h-[10px] bg-yellow-300 rounded-full -translate-x-1/2 -translate-y-1/2"
				:ref="'LED' + led.id_led"
				:style="{
					left: (led.x_led * gridSize.cellSizeX) + gridSize.cellSizeX / 2 + 'px',
					top: (((storeData.ylength_store - 1) - led.y_led) * gridSize.cellSizeY) + gridSize.cellSizeY / 2 + 'px'}"
				:class="{ 'hidden': led?.status == 'delete' }"
				@mousedown.left="startDragging(led, 'led')"
				@contextmenu.prevent="(event) => selectLed(led, event)"
				@contextmenu.stop>
			</div>
			<template v-if="showLedId">
				<div v-for="led in ledEdition" :key="led.id_led" class="absolute z-[20] select-none"
					:style="{
						left: (led.x_led * gridSize.cellSizeX) + gridSize.cellSizeX / 2 + 10 + 'px',
						top: (((storeData.ylength_store - 1) - led.y_led) * gridSize.cellSizeY) + gridSize.cellSizeY / 2 + 8 + 'px'}"
					:class="{ 'hidden': led?.status == 'delete' }">
					{{ led.mqtt_led_id }}
				</div>
			</template>
			<div v-for="box in boxEdition" :key="box.id_box" class="absolute z-[10] border-2 border-[rgb(9,255,0)] overflow-hidden" :ref="'BOX' + box.id_box"
				:style="{
					left: (box.xstart_box * gridSize.cellSizeX) + 'px',
					top: (((storeData.ylength_store) - box.yend_box) * gridSize.cellSizeY) + 'px',
					width: ((box.xend_box - box.xstart_box) * (gridSize.cellSizeX)) + 'px',
					height: ((box.yend_box - box.ystart_box) * (gridSize.cellSizeY)) + 'px'}"
				:class="{ 'hidden': box?.status == 'delete' }"
				@mousedown.left="startDragging(box, 'box')"
				@contextmenu.prevent="(event) => selectBox(box, event)"
				@contextmenu.stop>
				<template v-if="canEdit && !showMenu && (selectedElement.type == null || selectedElement.key == box)">
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -top-[5px] -left-[5px] cursor-nw-resize" @mousedown.left="startDragging(box, 'border', 'nw')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -top-[5px] -right-[5px] cursor-ne-resize" @mousedown.left="startDragging(box, 'border', 'ne')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -bottom-[5px] -left-[5px] cursor-sw-resize" @mousedown.left="startDragging(box, 'border', 'sw')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -bottom-[5px] -right-[5px] cursor-se-resize" @mousedown.left="startDragging(box, 'border', 'se')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent h-[10px] left-[5px] right-[5px] -top-[5px] cursor-n-resize" @mousedown.left="startDragging(box, 'border', 'n')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent w-[10px] top-[5px] bottom-[5px] -right-[5px] cursor-e-resize" @mousedown.left="startDragging(box, 'border', 'e')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent h-[10px] left-[5px] right-[5px] -bottom-[5px] cursor-s-resize" @mousedown.left="startDragging(box, 'border', 's')"
						@contextmenu.stop>
					</div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent w-[10px] top-[5px] bottom-[5px] -left-[5px] cursor-w-resize" @mousedown.left="startDragging(box, 'border', 'w')"
						@contextmenu.stop>
					</div>
				</template>
				<template v-else>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -top-[5px] -left-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -top-[5px] -right-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -bottom-[5px] -left-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] w-[10px] h-[10px] -bottom-[5px] -right-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent h-[10px] left-[5px] right-[5px] -top-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent w-[10px] top-[5px] bottom-[5px] -right-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent h-[10px] left-[5px] right-[5px] -bottom-[5px]"></div>
					<div class="absolute z-[11] bg-[#2c3e50] bg-transparent w-[10px] top-[5px] bottom-[5px] -left-[5px]"></div>	
				</template>	
			</div>
			<div v-if="showMenu" class="absolute bg-white border p-2 z-[100]" ref="menuModal"
				:style="{ left: menuPos.Xpx + 'px', top: menuPos.Ypx + 'px' }">
				<div class="flex flex-col">
					<template v-if="!selectedElement.type">
						<button @click="addLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreAddLed') }}
						</button>
						<button @click="addBox" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreAddBox') }}
						</button>
					</template>
					<template v-if="selectedElement.type == 'led'">
						<button v-if="canEdit" @click="deleteElement" class="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600">
							{{ $t('store.VStoreDeleteLed') }}
						</button>
						<button @click="toggleLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreToggleLed') }}
						</button>
						<div class="flex space-x-4">
							<span>{{ $t('store.VStoreMqttLedId') }}</span>
							<input type="number" v-model="ledEdition[selectedElement.key.id_led].mqtt_led_id" class="w-16" :disabled="!canEdit" />
						</div>
						<!-- TODO : add color weel and select animation and light duration -->
					</template>
					<template v-if="selectedElement.type == 'box'">
						<button v-if="canEdit" @click="deleteElement" class="bg-red-500 text-white px-4 py-1 rounded hover:bg-red-600">
							{{ $t('store.VStoreDeleteBox') }}
						</button>
						<button @click="showBoxContent" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreShowBoxContent') }}
						</button>
						<button @click="toggleBoxLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreToggleBoxLed') }}
						</button>
						<button v-if="canEdit" @click="addLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreAddLed') }}
						</button>
					</template>
				</div>
			</div>
			<div>
				<span>X: </span>
				<strong>{{ mousePos.X }}</strong>
				<span>Y: </span>
				<strong>{{ mousePos.Y }}</strong>
			</div>
		</template>
		<template v-else>
			<!-- TODO : add loading animation -->
		</template>
	</div>
</template>

<script>
export default {
	name: "Tableau",
	mounted() {
		document.addEventListener("click", (event) => this.hideMenu(event));
	},
	beforeUnmount() {
		document.removeEventListener("click", (event) => this.hideMenu(event));
	},
	props: {
		storeData: {
			type: Object,
			required: true,
		},
		ledEdition: {
			type: Object,
			required: true,
		},
		boxEdition: {
			type: Object,
			required: true,
		},
		canEdit: {
			type: Boolean,
			required: true,
		},
	},
	methods: {
		addLed() {
			this.storesStore.pushLed({
				id_led: this.getLastLedId() + 1,
				x_led: this.mouseClick.X,
				y_led: this.mouseClick.Y,
				mqtt_led_id: this.getLastLedMqttId() + 1,
				status: "new",
			});
			this.showMenu = false;
		},
		addBox() {
			this.storesStore.pushBox({
				id_box: this.getLastBoxId() + 1,
				xstart_box: this.mouseClick.X,
				ystart_box: this.mouseClick.Y,
				xend_box: this.mouseClick.X + 1,
				yend_box: this.mouseClick.Y + 1,
				status: "new",
			});
			this.showMenu = false;
		},
		getLastBoxId() {
			let max = 0;
			Object.keys(this.boxEdition).forEach((box) => {
				if (this.boxEdition[box].id_box > max) {
					max = this.boxEdition[box].id_box;
				}
			});
			return max;
		},
		getLastLedId() {
			let max = 0;
			Object.keys(this.ledEdition).forEach((led) => {
				if (this.ledEdition[led].id_led > max) {
					max = this.ledEdition[led].id_led;
				}
			});
			return max;
		},
		getLastLedMqttId() {
			let max = 0;
			Object.keys(this.ledEdition).forEach((led) => {
				if (this.ledEdition[led].mqtt_led_id > max) {
					max = this.ledEdition[led].mqtt_led_id;
				}
			});
			return max;
		},
		isNumber(value) {
			return typeof value === "number" && !isNaN(value);
		},
		showMenuModal(event, isNewElement = false) {
			if (isNewElement && !this.canEdit) {
				return;
			}
			this.showMenu = true;
			this.menuPos.Xpx = event.layerX;// TODO change type for editing
			this.menuPos.Ypx = event.layerY;// TODO change type for editing
		},
		hideMenu(event) {
			this.showMenu = false;
			this.stopSelecting();
		},
		saveMouseClickPos() {
			this.mouseClick.X = this.mousePos.X;
			this.mouseClick.Y = this.mousePos.Y;
		},
		stopSelecting() {
			document.querySelector(".selectedElement")?.classList.remove("selectedElement");
			document.querySelector(".cursor-move")?.classList.remove("cursor-move");
			document.querySelector(".diagonal-hatch")?.classList.remove("diagonal-hatch");
			if (this.selectedElement.type === "border") {
				document.querySelector(".grid")?.classList.remove("cursor-nw-resize");
				document.querySelector(".grid")?.classList.remove("cursor-ne-resize");
				document.querySelector(".grid")?.classList.remove("cursor-sw-resize");
				document.querySelector(".grid")?.classList.remove("cursor-se-resize");
				document.querySelector(".grid")?.classList.remove("cursor-n-resize");
				document.querySelector(".grid")?.classList.remove("cursor-e-resize");
				document.querySelector(".grid")?.classList.remove("cursor-s-resize");
				document.querySelector(".grid")?.classList.remove("cursor-w-resize");
			}
			this.selectedElement = { type: null, key: null, temp: {} };
		},
		selectLed(led, event = null) {
			if (event) {
				this.showMenuModal(event);
			} // if right click show the menu
			this.saveMouseClickPos();
			if (this.selectedElement.key === led) {
				return;
			} // if click on the element already selected don't do anything
			this.stopSelecting(); // unselect if another element is selected
			this.selectedElement.key = led;
			this.selectedElement.type = "led";
			if (led?.status !== "new") {
				led.status = "modified";
			}
			let ledHtml = this.$refs["LED" + led.id_led][0];
			ledHtml.classList.add("selectedElement");
		},
		selectBox(box, event = null) {
			if (event) {
				this.showMenuModal(event);
			} // if right click show the menu
			this.saveMouseClickPos();
			if (this.selectedElement.key === box) {
				return;
			} // if click on the element already selected don't do anything
			this.stopSelecting(); // unselect if another element is selected
			this.selectedElement.key = box;
			this.selectedElement.type = "box";
			// on stock la valeur de départ de la box
			this.selectedElement.temp.xstart_box = box.xstart_box;
			this.selectedElement.temp.ystart_box = box.ystart_box;
			this.selectedElement.temp.xend_box = box.xend_box;
			this.selectedElement.temp.yend_box = box.yend_box;
			this.selectedElement.status = "modified";
			if (box?.status !== "new") {
				box.status = "modified";
			}
			let boxHtml = this.$refs["BOX" + box.id_box][0];
			boxHtml.classList.add("selectedElement");
			boxHtml.classList.add("diagonal-hatch");
		},
	},
	emits: [
		"openBoxContent",
	],
	computed: {
		getGridCss() {
			return `grid-template-columns: repeat(${this.storeData.xlength_store}, ${this.gridSize.cellSizeX}px);
					grid-template-rows: repeat(${this.storeData.ylength_store}, ${this.gridSize.cellSizeY}px);
					height: ${this.storeData.ylength_store * this.gridSize.cellSizeY}px;
					width: ${this.storeData.xlength_store * this.gridSize.cellSizeX}px;
					`;
		},
	},
	data() {
		return {
			showMenu: false,
			menuPos: { Xpx: 0, Ypx: 0 },
			mousePos: { X: 0, Y: 0 },
			mouseClick: { X: 0, Y: 0 },
			gridSize: { cellSizeX: 40, cellSizeY: 40 },
			showLedId: true,
			selectedElement: { type: null, key: null, temp: {} },
		};
	},
	
};
</script>

<style>
.conflict {
	border-color: red;
}

.selectedElement {
	background-color: rgb(0, 174, 255);
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
</style>