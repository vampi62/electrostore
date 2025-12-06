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
						<button v-if="ledEdition[selectedElement.key.id_led].status != 'new'" @click="toggleLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
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
						<button v-if="boxEdition[selectedElement.key.id_box].status != 'new'" @click="$emit('openBoxContent', selectedElement.key.id_box)" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
							{{ $t('store.VStoreShowBoxContent') }}
						</button>
						<button v-if="boxEdition[selectedElement.key.id_box].status != 'new'" @click="toggleBoxLed" class="bg-blue-500 text-white px-4 py-1 rounded hover:bg-blue-600">
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
				<label class="ml-4">
					<input type="checkbox" v-model="showLedId" />
					{{ $t('store.VStoreShowLedId') }}
				</label>
			</div>
		</template>
		<template v-else>
			<!-- TODO : add loading animation -->
		</template>
	</div>
</template>

<script>
import { inject } from "vue";
export default {
	name: "Tableau",
	mounted() {
		document.addEventListener("click", (event) => this.hideMenu(event));
		document.addEventListener("mouseup", (event) => this.stopDragging(event));
	},
	beforeUnmount() {
		document.removeEventListener("click", (event) => this.this.hideMenu(event));
		document.removeEventListener("mouseup", (event) => this.stopDragging(event));
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
		storeFunc: {
			type: Object,
			required: true,
		},
	},
	methods: {
		addLed() {
			this.ledEdition[this.getLastLedId() + 1] = {
				id_led: this.getLastLedId() + 1,
				x_led: this.mouseClick.X,
				y_led: this.mouseClick.Y,
				mqtt_led_id: this.getLastLedMqttId() + 1,
				status: "new",
			};
			this.showMenu = false;
		},
		addBox() {
			this.boxEdition[this.getLastBoxId() + 1] = {
				id_box: this.getLastBoxId() + 1,
				xstart_box: this.mouseClick.X,
				ystart_box: this.mouseClick.Y,
				xend_box: this.mouseClick.X + 1,
				yend_box: this.mouseClick.Y + 1,
				status: "new",
			};
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
			return typeof value === "number" && !Number.isNaN(value);
		},
		showMenuModal(event, isNewElement = false) {
			if (isNewElement && !this.canEdit) {
				return;
			}
			this.showMenu = true;
			this.saveMouseClickPos();
			if (isNewElement) {
				this.menuPos.Xpx = event.layerX;
				this.menuPos.Ypx = event.layerY;
			} else {
				this.menuPos.Xpx = event.layerX + event.target.offsetLeft;
				this.menuPos.Ypx = event.layerY + event.target.offsetTop;
			}
		},
		hideMenu(event) {
			if (this.$refs.menuModal?.contains(event.target)) {
				return;
			}
			this.showMenu = false;
			this.stopSelecting();
		},
		saveMouseClickPos() {
			this.mouseClick.X = this.mousePos.X;
			this.mouseClick.Y = this.mousePos.Y;
		},
		stopSelecting(event = null) {
			if (event !== null && this.$refs.menuModal?.contains(event.target)) {
				return;
			}
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
		selectBorder(border, direction) {
			if (this.selectedElement.key === border) {
				this.stopSelecting(); // unselect if click on the selectedElement element
				return;
			}
			this.stopSelecting(); // unselect if click on another element
			this.selectedElement.key = border;
			this.selectedElement.type = "border";
			// on stock la valeur de départ de la box lier au border
			this.selectedElement.temp.xstart_box = border.xstart_box;
			this.selectedElement.temp.ystart_box = border.ystart_box;
			this.selectedElement.temp.xend_box = border.xend_box;
			this.selectedElement.temp.yend_box = border.yend_box;
			this.selectedElement.temp.direction = direction;
			if (border?.status !== "new") {
				border.status = "modified";
			}
			let boxHtml = this.$refs["BOX" + border.id_box][0];
			boxHtml.classList.add("selectedElement");
			boxHtml.classList.add("diagonal-hatch");
		},
		deleteElement() {
			if (this.selectedElement.type === "led") {
				Object.keys(this.ledEdition).forEach((index) => {
					if (this.ledEdition[index] === this.selectedElement.key) {
						if (this.ledEdition[index].status === "new") {
							delete this.ledEdition[index];
						} else {
							this.ledEdition[index].status = "delete";
						}
					}
				});
			} else if (this.selectedElement.type === "box") {
				Object.keys(this.boxEdition).forEach((index) => {
					if (this.boxEdition[index] === this.selectedElement.key) {
						if (this.boxEdition[index].status === "new") {
							delete this.boxEdition[index];
						} else {
							this.boxEdition[index].status = "delete";
						}
					}
				});
			}
			this.showMenu = false;
			this.stopSelecting();
		},
		async toggleLed(ledId) {
			try {
				await this.storeFunc.showLedById(ledId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
				this.addNotification({ message: "store.VStoreLedShowSuccess", type: "success", i18n: true });
			} catch (e) {
				this.addNotification({ message: e, type: "error", i18n: false });
			}
		},
		async toggleBoxLed(boxId) {
			try {
				await this.storeFunc.showBoxById(boxId, { "red": 255, "green": 255, "blue": 255, "timeshow": 30, "animation": 4 });
				this.addNotification({ message: "store.VStoreBoxShowSuccess", type: "success", i18n: true });
			} catch (e) {
				this.addNotification({ message: e, type: "error", i18n: false });
			}
		},
		startDragging(element, type, direction = null) {
			if (!this.canEdit) {
				return;
			}
			if (this.hasDragElement) {
				return;
			}
			this.saveMouseClickPos();
			// if the element is not the selected element, change the selected element
			if ((this.selectedElement.key !== element) || (this.selectedElement.type !== type)) {
				if (type === "led") {
					this.selectLed(element);
					document.querySelector(".grid")?.classList.add("cursor-move");
				} else if (type === "box") {
					this.selectBox(element);
					document.querySelector(".grid")?.classList.add("cursor-move");
				} else if (type === "border") {
					this.selectBorder(element, direction);
					document.querySelector(".grid")?.classList.add("cursor-" + direction);
				}
			}
			this.hasDragElement = true;
			this.showMenu = false;
		},
		stopDragging() {
			if (!this.hasDragElement) {
				return;
			}
			this.hasDragElement = false;
		},
		moveMouse(event) {
			let gridBoxPos = document.querySelector(".grid").getBoundingClientRect();
			let left = gridBoxPos.left + window.scrollX;
			let top = gridBoxPos.top + window.scrollY;
			this.gridSize.cellSizeX = Math.floor(gridBoxPos.width / this.storeData.xlength_store);
			this.gridSize.cellSizeY = Math.floor(gridBoxPos.height / this.storeData.ylength_store);
			let x = Math.floor((event.clientX - left + window.scrollX) / this.gridSize.cellSizeX);
			let y = this.storeData.ylength_store - Math.floor((event.clientY - top + window.scrollY) / this.gridSize.cellSizeY) - 1;
			// if a value is out of the grid, don't update the mousePos
			if ((x < 0) || (y < 0) || (x >= this.storeData.xlength_store) || (y >= this.storeData.ylength_store)) {
				return;
			}
			this.mousePos.X = x;
			this.mousePos.Y = y;
			if (!this.hasDragElement) {
				return;
			}
			if (this.selectedElement.type === "led") {
				this.selectedElement.key.x_led = x;
				this.selectedElement.key.y_led = y;
			} else if (this.selectedElement.type === "box") {
				let dx = x - this.mouseClick.X;
				let dy = y - this.mouseClick.Y;
				if ((this.selectedElement.temp.xstart_box + dx < 0) ||
					(this.selectedElement.temp.ystart_box + dy < 0) ||
					(this.selectedElement.temp.xend_box + dx > this.storeData.xlength_store) ||
					(this.selectedElement.temp.yend_box + dy > this.storeData.ylength_store)) {
					return;
				}
				this.selectedElement.key.xstart_box = this.selectedElement.temp.xstart_box + dx;
				this.selectedElement.key.ystart_box = this.selectedElement.temp.ystart_box + dy;
				this.selectedElement.key.xend_box = this.selectedElement.temp.xend_box + dx;
				this.selectedElement.key.yend_box = this.selectedElement.temp.yend_box + dy;
				this.checkBoxConflict();
			} else if (this.selectedElement.type === "border") {
				if ((this.selectedElement.temp.direction === "ne" || this.selectedElement.temp.direction === "se" || this.selectedElement.temp.direction === "e") &&
					(this.mousePos.X >= this.selectedElement.temp.xstart_box)) {
					this.selectedElement.key.xend_box = this.selectedElement.temp.xend_box + (this.mousePos.X - this.mouseClick.X);
				} else if ((this.selectedElement.temp.direction === "nw" || this.selectedElement.temp.direction === "ne" || this.selectedElement.temp.direction === "n") &&
					(this.mousePos.Y >= this.selectedElement.temp.ystart_box)) {
					this.selectedElement.key.yend_box = this.selectedElement.temp.yend_box + (this.mousePos.Y - this.mouseClick.Y);
				} else if ((this.selectedElement.temp.direction === "nw" || this.selectedElement.temp.direction === "sw" || this.selectedElement.temp.direction === "w") &&
					(this.mousePos.X < this.selectedElement.temp.xend_box)) {
					this.selectedElement.key.xstart_box = this.selectedElement.temp.xstart_box + (this.mousePos.X - this.mouseClick.X);
				} else if ((this.selectedElement.temp.direction === "sw" || this.selectedElement.temp.direction === "se" || this.selectedElement.temp.direction === "s") &&
					(this.mousePos.Y < this.selectedElement.temp.yend_box)) {
					this.selectedElement.key.ystart_box = this.selectedElement.temp.ystart_box + (this.mousePos.Y - this.mouseClick.Y);
				}
				this.checkBoxConflict();
			}
		},
		checkBoxConflict(validate = false) {
			Object.values(this.boxEdition).forEach((box) => {
				this.$refs["BOX" + box.id_box][0].classList.remove("conflict");
			});
			Object.values(this.boxEdition).forEach((box1) => {
				Object.values(this.boxEdition).forEach((box2) => {
					if (box1.id_box !== box2.id_box) {
						if ((box1.xstart_box < box2.xend_box) &&
						(box1.xend_box > box2.xstart_box) &&
						(box1.ystart_box < box2.yend_box) &&
						(box1.yend_box > box2.ystart_box)) {
							this.$refs["BOX" + box1.id_box][0].classList.add("conflict");
							this.$refs["BOX" + box2.id_box][0].classList.add("conflict");
						}
					}
				});
			});
			if (validate) {
				let BreakException = {};
				try {
					Object.values(this.boxEdition).forEach((box) => {
						if (this.$refs["BOX" + box.id_box][0].classList.contains("conflict")) {
							this.addNotification({ message: "store.VStoreBoxConflict", type: "error", i18n: true });
							throw BreakException;
						}
					});
					return true;
				} catch (e) {
					return false;
				}
			}
		},
		checkOutOfGrid() {
			let errorLed = false;
			Object.keys(this.ledEdition).forEach((led) => {
				if ((this.ledEdition[led].x_led >= this.storeData.xlength_store) || (this.ledEdition[led].y_led >= this.storeData.ylength_store)) {
					if (this.ledEdition[led]?.status !== "delete") {
						errorLed = true;
					}
				}
			});
			if (errorLed) {
				this.addNotification({ message: "store.VStoreLedOutOfGrid", type: "error", i18n: true });
				return false;
			}
			let errorBox = false;
			Object.keys(this.boxEdition).forEach((box) => {
				if ((this.boxEdition[box].xend_box > this.storeData.xlength_store) || (this.boxEdition[box].yend_box > this.storeData.ylength_store)) {
					if (this.boxEdition[box]?.status !== "delete") {
						errorBox = true;
					}
				}
			});
			if (errorBox) {
				this.addNotification({ message: "store.VStoreBoxOutOfGrid", type: "error", i18n: true });
				return false;
			}
			return true;
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
	setup() {
		const { addNotification } = inject("useNotification"); 
		return {
			addNotification,
		};
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
			hasDragElement: false,
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