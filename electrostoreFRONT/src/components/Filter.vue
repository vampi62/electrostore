<template>
	<div v-if="type !== 'hidden'">
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
					<option v-for="option in filterOption" :key="option.id" :value="option.id" :selected="preset === option.id">{{ option.value }}
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
					@input="storeData && storeKey ? debouncedRefetchData() : null"
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
					<template v-if="options && !storeData">
						<div v-for="option in filterOption" :key="option.id"
							@mousedown.prevent="selectOption(option.id, option.value)"
							class="flex flex-col p-2 hover:bg-gray-100 cursor-pointer">
							<span class="text-sm">{{ option.value }}</span>
						</div>
					</template>
					<template v-else>
						<div v-for="[index, option] in filterStoreOption" :key="index"
							@mousedown.prevent="selectOption(index, option)"
							class="flex flex-col p-2 hover:bg-gray-100 cursor-pointer">
							<span class="text-sm">{{ option }}</span>
						</div>
					</template>
				</div>
			</teleport>
		</template>
		<template v-else-if="type === 'checkbox'">
			<input
				:id="`filter-input-${this.$.uid}`"
				type="checkbox"
				class="form-checkbox h-5 w-5 text-blue-600"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				v-model="inputText"
				@change="$emit('updateText', $event.target.checked)" />
		</template>
		<template v-else>
			<input
				:id="`filter-input-${this.$.uid}`"
				:type="type"
				:placeholder="placeholder"
				:value="preset"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				@input="$emit('updateText', $event.target.value)" />
		</template>
		</div>
	</div>
</template>

<script>
import { nextTick } from "vue";
import { debounce } from "lodash-es";
import { buildRSQLFilter, buildRSQLSort } from "@/utils";
import { toLowerCaseWithoutAccents } from "@/utils";
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
			// 'text', 'number', 'select', 'datalist', etc.
			default: "text",
		},
		placeholder: {
			type: String,
			required: false,
			// This should be already translated texte
			// e.g., 'write here your text'
			default: "",
		},
		preset: {
			type: [String, Number],
			required: false,
			// This should be the value to preset the input with
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
			// This should be an array of options for select/datalist input
			// e.g., [{id: 'id1', value: 'Option 1'}, {id: 'id2', value: 'Option 2'}]
			// translate the values before passing
			// to the component
			default: () => ([]),
		},
		sortOptions: {
			type: String,
			required: false,
			// This should be a string indicating how to sort the options, e.g., 'asc' or 'desc'
			default: null,
		},
		fetchOptions: {
			type: Function,
			required: false,
			// This should be a function that returns a promise resolving to an array of options
			// e.g., () => fetch('/api/options').then(res => res.json())
			default: (limit, offset, expand, filter, sort, clear) => { 
				return [0, false];
			},
		},
		storeData: {
			type: Object,
			required: false,
			// This should be a pinia store whose data will be received by the fetchOptions function
			default: null,
		},
		storeKey: {
			type: String,
			required: false,
			// This should be the key in storeData to pass to fetchOptions function
			default: null,
		},
	},
	data() {
		return {
			isOpen: false,
			inputText: this.preset,
		};
	},
	created() {
		this.debouncedRefetchData = debounce(this.refetchData, 500);
	},
	mounted() {
		if (this.type === "datalist" && this.preset) {
			const found = this.options && this.options.find((o) => String(o.id) === String(this.preset));
			if (found) {
				this.inputText = found.value;
			} else if (this.storeData && this.storeKey) {
				this.inputText = this.preset;
			}
		}
		if (this.type === "datalist" && this.fetchOptions && this.storeData && this.storeKey) {
			this.refetchData();
		}
	},
	emits: ["updateText"],
	computed: {
		filterOption() {
			if (!this.options) {
				return [];
			}
			let result = this.options.filter((option) => {
				if (this.inputText !== "") {
					return toLowerCaseWithoutAccents(String(option.value)).includes(toLowerCaseWithoutAccents(this.inputText));
				}
				return true;
			});
			if (this.sortOptions) {
				result = result.slice().sort((a, b) => {
					const aVal = toLowerCaseWithoutAccents(String(a.value));
					const bVal = toLowerCaseWithoutAccents(String(b.value));
					return this.sortOptions === "desc" ? bVal.localeCompare(aVal) : aVal.localeCompare(bVal);
				});
			}
			return result;
		},
		filterStoreOption() {
			if (!this.storeData || !this.storeKey) {
				return [];
			}
			return Object.entries(this.storeData).filter(([index, element]) => {
				if (this.inputText !== "") {
					return toLowerCaseWithoutAccents(element[this.storeKey]).includes(toLowerCaseWithoutAccents(this.inputText));
				}
				return true;
			}).map(([index, element]) => {
				return [element[this.storeKey], element[this.storeKey]];
			});
		},
	},
	methods: {
		selectOption(index, option){
			this.inputText = option;
			this.isOpen = false;
			this.$emit("updateText", index);
			this.$refs.filterInput.blur();
		},
		validateInput(){
			if (this.options && this.options.length > 0 && !this.storeData) {
				const result = this.options.find((option) => {
					return toLowerCaseWithoutAccents(String(option.value)) === toLowerCaseWithoutAccents(this.inputText);
				});
				if (result) {
					this.inputText = result.value;
					this.$emit("updateText", result.id);
				} else {
					this.inputText = "";
					this.$emit("updateText", "");
				}
			} else if (this.storeData && this.storeKey) {
				this.$emit("updateText", this.inputText);
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
		async refetchData() {
			const filter = [{
				key: this.storeKey,
				compareMethod: "=like=",
				value: this.inputText,
			}];
			const sort = {
				key: this.storeKey,
				order: "asc",
			};
			await this.fetchOptions(10, 0, [], buildRSQLFilter(filter), buildRSQLSort(sort), false);
		},
	},
};
</script>