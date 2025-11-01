<template>
	<div>
		<label v-if="label.length > 0" :for="`filter-input-${this.$.uid}`" class="text-sm text-gray-700 mr-2">{{ $t(label) }}</label>
		<div>
		<template v-if="type === 'select'">
			<select
				:id="`filter-input-${this.$.uid}`"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				@change="$emit('updateText', $event.target.value)">
				<option value=""></option>
				<template v-if="options">
					<option v-for="option in options" :key="option[0]" :value="option[0]">{{ option[1] }}
					</option>
				</template>
			</select>
		</template>
		<template v-else-if="type === 'datalist'">
			<div class="relative max-w-xs mx-auto">
				<input
					:id="`filter-input-${this.$.uid}`"
					ref="filterInput"
					type="text"
					class="border border-gray-300 rounded px-2 py-1"
					:class="[classCss, label.length > 0 ? 'mr-2' : '']"
					:placeholder="placeholder"
					v-model="inputText"
					@focus="isOpen = true; inputText='', startEventUpdatePosition()"
					@blur="isOpen = false; validateInput(); endEventUpdatePosition()" />
				<div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
					<svg
						class="w-4 h-4 text-gray-500"
						fill="none"
						stroke="currentColor"
						viewBox="0 0 24 24"
						xmlns="http://www.w3.org/2000/svg">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M19 9l-7 7-7-7" />
					</svg>
				</div>
			</div>
			<teleport to="body">
				<div v-show="isOpen"
					:ref="`filterList`"
					class="absolute border max-h-48 overflow-y-auto bg-white left-0" style="width: calc(100% - 8px);">
					<div v-for="option in filterOption" :key="option[0]"
						@mousedown.prevent="selectOption(option)"
						class="flex flex-col p-2 hover:bg-gray-100 cursor-pointer">
						<span class="text-sm">{{ option[1] }}</span>
					</div>
				</div>
			</teleport>
		</template>
		<template v-else>
			<input
				:id="`filter-input-${this.$.uid}`"
				:type="type"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:placeholder="placeholder"
				@input="$emit('updateText', $event.target.value)" />
		</template>
		</div>
	</div>
</template>

<script>
import { nextTick } from "vue";
export default {
	name: "Filter",
	props: {
		label: {
			type: String,
			required: true,
			// This should be a translation key for the label
			// e.g., 'components.FilterLabel'
			default: "",
		},
		type: {
			type: String,
			required: true,
			// This should be a valid input type
			// 'text', 'number', 'select', etc.
			default: "text",
		},
		placeholder: {
			type: String,
			required: false,
			// This should be already translated texte
			// e.g., 'write here your text'
			default: "",
		},
		classCss: {
			type: String,
			required: false,
			// This should be a CSS tailwind class for styling the input
			default: "",
		},
		options: {
			type: Array,
			required: false,
			// This should be an array of options for select input
			// e.g., [['value1', 'Label 1'], ['value2', 'Label 2']]
			// translate the labels before passing
			// to the component
			default: () => [],
		},
	},
	data() {
		return {
			isOpen: false,
			inputText: "",
		};
	},
	emits: ["updateText"],
	computed: {
		filterOption() {
			return Object.values(this.options).filter((element) => {
				if (this.inputText !== "") {
					return element[1].toLowerCase().includes(this.inputText.toLowerCase());
				}
				return true;
			});
		},
	},
	methods: {
		selectOption(option){
			this.inputText = option[1];
			this.isOpen = false;
			this.$emit("updateText", option[0]);
			this.$refs.filterInput.blur();
		},
		validateInput(){
			let result = Object.values(this.options).find((option) => {
				return option[1].toLowerCase() === this.inputText.toLowerCase();
			});
			if (result) {
				this.inputText = result[1];
				this.$emit("updateText", result[0]);
			} else {
				this.inputText = "";
				this.$emit("updateText", "");
			}
		},
		startEventUpdatePosition(){
			window.addEventListener("scroll", this.updatePosition, true);
			window.addEventListener("resize", this.updatePosition);
			nextTick(() => {
				this.updatePosition();
			});
		},
		endEventUpdatePosition(){
			window.removeEventListener("scroll", this.updatePosition, true);
			window.removeEventListener("resize", this.updatePosition);
		},
		updatePosition(){
			if (!this.isOpen) {
				return;
			}
			let inputElement = this.$refs.filterInput;
			let listElement = this.$refs.filterList;
			if (inputElement && listElement) {
				let rect = inputElement.getBoundingClientRect();
				listElement.style.top = `${rect.bottom + window.scrollY}px`;
				listElement.style.left = `${rect.left + window.scrollX}px`;
				listElement.style.width = `${rect.width}px`;
			}
		},

	},
};
</script>