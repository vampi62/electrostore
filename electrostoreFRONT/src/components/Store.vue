<template>
	<div class="grid relative bg-color-200 border border-color-400"
		@contextmenu.prevent="showNewMenu" @mousemove="moveMouse"
		:class="getGridClass"
	>
		<template v-if="isNumber(storeData.xlength_store) && isNumber(storeData.ylength_store)">
			<div v-for="i in storeData.xlength_store * storeData.ylength_store"
				:key="i" class="cell"></div>
			<div v-for="led in ledEdition" :key="led.id_led" class="led"
				:id="'LED' + led.id_led"
				:style="{
					left: (led.x_led * gridSize.cellSizeX) + gridSize.cellSizeX / 2 + 'px',
					top: (((storeData.ylength_store - 1) - led.y_led) * gridSize.cellSizeY) + gridSize.cellSizeY / 2 + 'px',
					zIndex: 20}"
				:class="{ 'hidden': led?.status == 'delete' }"
				@mousedown.left="startDragging(led, 'led')"
				@contextmenu.prevent="(event) => selectLed(led, event)"
				@contextmenu.stop>
			</div>
			<template v-if="showLedId">
				<div v-for="led in ledEdition" :key="led.id_led" class="no-select"
					:style="{
						left: (led.x_led * gridSize.cellSizeX) + gridSize.cellSizeX / 2 + 10 + 'px',
						top: (((storeData.ylength_store - 1) - led.y_led) * gridSize.cellSizeY) + gridSize.cellSizeY / 2 + 8 + 'px',
						zIndex: 20,
						position: 'absolute'}"
					:class="{ 'hidden': led?.status == 'delete' }">
					{{ led.mqtt_led_id }}
				</div>
			</template>
			<div v-for="box in boxEdition" :key="box.id_box" class="box" :id="'BOX' + box.id_box"
				:style="{
					left: (box.xstart_box * gridSize.cellSizeX) + 'px',
					top: (((storeData.ylength_store) - box.yend_box) * gridSize.cellSizeY) + 'px',
					width: ((box.xend_box - box.xstart_box) * (gridSize.cellSizeX)) + 'px',
					height: ((box.yend_box - box.ystart_box) * (gridSize.cellSizeY)) + 'px',
					zIndex: 10}"
				:class="{ 'hidden': box?.status == 'delete' }"
				@mousedown.left="startDragging(box, 'box')"
				@contextmenu.prevent="(event) => selectBox(box, event)"
				@contextmenu.stop>
				<div v-if="canEdit && !showMenu && (selectedElement.type == null || selectedElement.key == box) ">
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
</template>
<script>
export default {
	name: "Tableau",
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
				id_led: 0,
				x_led: this.mouseClick.X,
				y_led: this.mouseClick.Y,
				mqtt_led_id: this.getLastLedMqttId() + 1,
				status: "new",
			});
			this.showMenu = false;
		},
		addBox() {
			this.storesStore.pushBox({
				id_box: 0,
				xstart_box: this.mouseClick.X,
				ystart_box: this.mouseClick.Y,
				xend_box: this.mouseClick.X + 1,
				yend_box: this.mouseClick.Y + 1,
				status: "new",
			});
			this.showMenu = false;
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
	},
	computed: {
		getGridClass() {
			if (this.isNumber(this.storeData.xlength_store) && this.isNumber(this.storeData.ylength_store)) {
				return `grid-cols-${this.storeData.xlength_store} grid-rows-${this.storeData.ylength_store}`;
			}
			return "";
		},
	},
	data() {
		return {
			showMenu: false,
			mouseClick: { X: 0, Y: 0 },
			gridSize: { cellSizeX: 40, cellSizeY: 40 },
			showLedId: true,
			selectedElement: { type: null, key: null },
		};
	},
};
</script>