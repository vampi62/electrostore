<template>
	<div v-if="type !== 'hidden'">
		<label v-if="label.length > 0" :for="`filter-input-${$.uid}`" class="text-sm text-gray-700 mr-2">{{ $t(label) }}</label>
		<div>
		<template v-if="type === 'select'">
			<select
				:id="`filter-input-${$.uid}`"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:disabled="disabled"
				@change="$emit('updateText', [($event.target as HTMLSelectElement).value, ($event.target as HTMLSelectElement).value])">
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
					:id="`filter-input-${$.uid}`"
					ref="filterInput"
					type="text"
					class="border border-gray-300 rounded px-2 py-1"
					:class="[classCss, label.length > 0 ? 'mr-2' : '']"
					:placeholder="placeholder"
					:disabled="disabled"
					v-model="inputText"
					@input="storeData && storeKey ? debouncedRefetchData?.() : null"
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
				:id="`filter-input-${$.uid}`"
				type="checkbox"
				class="form-checkbox h-5 w-5 text-blue-600"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:disabled="disabled"
				v-model="inputText"
				@change="$emit('updateText', [($event.target as HTMLInputElement).checked, ($event.target as HTMLInputElement).checked])" />
		</template>
		<template v-else>
			<input
				:id="`filter-input-${$.uid}`"
				:type="type"
				:placeholder="placeholder"
				:value="preset"
				autocomplete="off"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:disabled="disabled"
				@input="$emit('updateText', [($event.target as HTMLInputElement).value, ($event.target as HTMLInputElement).value])" />
		</template>
		</div>
	</div>
</template>

<script lang="ts">
import { nextTick } from "vue";
import type { PropType } from "vue";
import type { StateTree } from "pinia";
import { debounce } from "lodash-es";
import { toLowerCaseWithoutAccents } from "@/utils";
import type { FetchFunction, FetchPage } from "@/types/fetch";
import type { FilterOption, FilterStrictMode, FilterValue } from "@/types/filter";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";
export default {
	name: "Filter",
	props: {
		label: {
			type: String,
			required: true,
			// This should be a translation key for the label
			// e.g., 'components.FilterLabel'
		},
		type: {
			type: String,
			required: true,
			// This should be a valid input type
			// 'text', 'number', 'select', 'datalist', etc.
		},
		disabled: {
			type: Boolean,
			required: false,
			default: false,
		},
		placeholder: {
			type: String,
			required: false,
			// This should be already translated texte
			// e.g., 'write here your text'
			default: "",
		},
		preset: {
			type: [String, Number, Boolean] as PropType<FilterValue>,
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
			type: Array as PropType<FilterOption[]>,
			required: false,
			// This should be an array of options for select/datalist input
			// e.g., [{id: 'id1', value: 'Option 1'}, {id: 'id2', value: 'Option 2'}]
			// translate the values before passing
			// to the component
			default: () => ([]),
		},
		sortOptions: {
			type: String as PropType<"asc" | "desc">,
			required: false,
			// This should be a string indicating how to sort the options, e.g., 'asc' or 'desc'
			default: undefined,
		},
		fetchOptions: {
			type: Function as PropType<FetchFunction>,
			required: false,
			// This should be a function that loads the options of a datalist in the store of storeData
			// e.g., (limit, offset, expand, filter, sort, clear) => store.fetchData(limit, offset, expand, filter, sort, clear)
			default: (): FetchPage => {
				return [0, false];
			},
		},
		storeData: {
			type: Object as PropType<StateTree>,
			required: false,
			// This should be the state of a pinia store whose data are the options, loaded by the fetchOptions function
			default: undefined,
		},
		storeKey: {
			type: String,
			required: false,
			// This should be the key in storeData of the value displayed, it is also the key sent to fetchOptions function
			default: undefined,
		},
		strictMode: {
			type: Object as PropType<FilterStrictMode>,
			required: false,
			// This should be an object containing the configuration for strict mode
			// e.g., { key: 'region_signaleur.id', storeKey: 'id', typeData: 'number' }
			default: undefined,
		},
	},
	data() {
		return {
			isOpen: false,
			inputText: this.preset,
			debouncedRefetchData: null as (() => void) | null,
			justSelected: false,
		};
	},
	created() {
		this.debouncedRefetchData = debounce(this.refetchData, 500);
		this.justSelected = false;
	},
	async mounted() {
		if (this.type === "datalist" && this.preset) {
			const found = this.options && this.options.find((o) => String(o.id) === String(this.preset));
			if (found) {
				this.inputText = found.value;
			} else if (this.storeData && this.storeKey) {
				this.inputText = this.preset;
			}
		}
		if (this.type === "datalist" && this.storeData && this.storeKey) {
			await this.refetchData();
			const valueKey = this.strictMode ? (this.strictMode.storeKey ?? "") : this.storeKey;
			const foundInStore = Object.values(this.storeData).find((o) => String(o[valueKey]) === String(this.preset));
			if (foundInStore) {
				this.inputText = foundInStore[valueKey];
			}
		}
	},
	emits: {
		// value is [value, displayed text], mode "select" is sent when an option of a strictMode filter is chosen
		updateText: (_value: [FilterValue, FilterValue], _mode?: "text" | "select") => true,
	},
	computed: {
		filterOption(): FilterOption[] {
			if (!this.options) {
				return [];
			}
			let result = this.options.filter((option) => {
				if (this.inputText !== null && this.inputText !== "") {
					return toLowerCaseWithoutAccents(String(option.value)).includes(toLowerCaseWithoutAccents(String(this.inputText)));
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
		// [value, displayed text] of each element of storeData
		filterStoreOption(): [string | number, string][] {
			if (!this.storeData || !this.storeKey) {
				return [];
			}
			const storeKey = this.storeKey;
			const valueKey = this.strictMode ? (this.strictMode.storeKey ?? "") : storeKey;
			let result = Object.values(this.storeData).filter((element) => {
				if (this.inputText !== null && this.inputText !== "") {
					return toLowerCaseWithoutAccents(element[storeKey]).includes(toLowerCaseWithoutAccents(String(this.inputText)));
				}
				return true;
			}).map((element): [string | number, string] => [element[valueKey], element[storeKey]]);
			if (this.sortOptions) {
				result = result.slice().sort((a, b) => {
					const aVal = toLowerCaseWithoutAccents(String(a[1]));
					const bVal = toLowerCaseWithoutAccents(String(b[1]));
					return this.sortOptions === "desc" ? bVal.localeCompare(aVal) : aVal.localeCompare(bVal);
				});
			}
			return result;
		},
	},
	methods: {
		selectOption(index: string | number, option: string){
			this.inputText = option;
			this.isOpen = false;
			if (this.strictMode) {
				this.justSelected = true;
				this.$emit("updateText", [index, option], "select");
			} else {
				this.$emit("updateText", [index, option], "text");
			}
			(this.$refs.filterInput as HTMLInputElement).blur();
		},
		validateInput(){
			if (this.justSelected) {
				this.justSelected = false;
				return;
			}
			if (this.options && this.options.length > 0 && !this.storeData) {
				const result = this.options.find((option) => {
					return toLowerCaseWithoutAccents(String(option.value)) === toLowerCaseWithoutAccents(String(this.inputText));
				});
				if (result) {
					this.inputText = result.value;
					this.$emit("updateText", [result.id, result.value]);
				} else {
					this.inputText = "";
					this.$emit("updateText", ["", ""]);
				}
			} else if (this.storeData && this.storeKey) {
				this.$emit("updateText", [this.inputText, this.inputText]);
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
			let inputElement = this.$refs.filterInput as HTMLInputElement | undefined;
			let listElement = this.$refs.filterList as HTMLElement | undefined;
			if (inputElement && listElement) {
				let rect = inputElement.getBoundingClientRect();
				listElement.style.top = `${rect.bottom + window.scrollY}px`;
				listElement.style.left = `${rect.left + window.scrollX}px`;
				listElement.style.width = `${rect.width}px`;
			}
		},
		async refetchData() {
			const filter: RSQLFilter[] = [{
				key: this.storeKey ?? "",
				compareMethod: "=like=",
				value: this.inputText,
			}];
			const sort: RSQLSort = {
				key: this.storeKey,
				order: "asc",
			};
			await this.fetchOptions(10, 0, [], filter, sort, false);
		},
	},
};
</script>
