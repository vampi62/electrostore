<template>
	<div v-if="type !== 'hidden'">
		<label v-if="label.length > 0" :for="inputId" class="text-sm text-gray-700 mr-2">{{ $t(label) }}</label>
		<div>
			<template v-if="isList">
				<div
					ref="filterBox"
					class="relative flex flex-wrap items-center gap-1 border border-gray-300 rounded px-2 py-1 pr-8 bg-white"
					:class="[classCss, label.length > 0 ? 'mr-2' : '', disabled ? 'opacity-50' : '']">
					<span v-for="item in chips" :key="String(item.value)"
						class="flex items-center gap-1 bg-blue-100 text-blue-800 rounded px-2 text-sm">
						{{ item.text }}
						<button type="button" class="font-bold hover:text-blue-950" :disabled="disabled"
							@mousedown.prevent="removeValue(item.value)">&times;</button>
					</span>
					<input
						:id="inputId"
						ref="filterInput"
						type="text"
						class="flex-1 min-w-24 outline-none bg-transparent"
						:placeholder="selected.length === 0 || !multiple ? placeholder : ''"
						:disabled="disabled"
						autocomplete="off"
						v-model="query"
						@input="onInput"
						@focus="onFocus"
						@blur="onBlur"
						@keydown.backspace="onBackspace" />
					<svg
						class="w-4 h-4 text-gray-500 absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none"
						fill="none"
						stroke="currentColor"
						viewBox="0 0 24 24"
						xmlns="http://www.w3.org/2000/svg">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
					</svg>
				</div>
				<teleport to="body">
					<div v-show="isOpen" ref="filterList"
						class="absolute z-50 border max-h-48 overflow-y-auto bg-white left-0">
						<div v-for="option in listOptions" :key="String(option.value)"
							class="flex flex-col p-2 hover:bg-gray-100 cursor-pointer"
							@mousedown.prevent="selectOption(option)">
							<span class="text-sm">{{ option.text }}</span>
						</div>
					</div>
				</teleport>
			</template>
			<input
				v-else-if="type === 'checkbox'"
				:id="inputId"
				type="checkbox"
				class="form-checkbox h-5 w-5 text-blue-600"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:disabled="disabled"
				:checked="modelValue === true"
				@change="$emit('update', ($event.target as HTMLInputElement).checked)" />
			<input
				v-else
				:id="inputId"
				:type="type"
				:placeholder="placeholder"
				:value="text"
				autocomplete="off"
				class="border border-gray-300 rounded px-2 py-1"
				:class="[classCss, label.length > 0 ? 'mr-2' : '']"
				:disabled="disabled"
				@input="onTextInput" />
		</div>
	</div>
</template>

<script lang="ts">
import { nextTick } from "vue";
import type { PropType } from "vue";
import type { StateTree } from "pinia";
import { debounce } from "lodash-es";
import { toLowerCaseWithoutAccents } from "@/utils";
import { filledValues } from "@/helpers";
import type { FetchFunction, FetchPage } from "@/types/fetch";
import type { FilterOption, FilterValue } from "@/types/filter";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

// value sent to the filter and text displayed for an option of a datalist
interface ListItem {
	value: string | number | boolean;
	text: string;
}

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
			// 'text', 'number', 'datalist', 'checkbox', etc.
		},
		modelValue: {
			type: [String, Number, Boolean, Array] as PropType<RSQLFilter["value"]>,
			required: false,
			// The current value of the filter, an array if the filter is multiple.
			// Only used to initialize the text inputs, a datalist and a checkbox always display it
			default: "",
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
		classCss: {
			type: String,
			required: false,
			// This should be a CSS tailwind class for styling the input
			default: "",
		},
		options: {
			type: Array as PropType<FilterOption[]>,
			required: false,
			// This should be an array of options for a datalist
			// e.g., [{id: 'id1', value: 'Option 1'}, {id: 'id2', value: 'Option 2'}]
			// translate the values before passing to the component
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
			// This should be the key in storeData of the value displayed and filtered, it is also the key sent to fetchOptions function
			default: undefined,
		},
		multiple: {
			type: Boolean,
			required: false,
			// Several options can be selected, the value is then an array
			default: false,
		},
	},
	data() {
		return {
			isOpen: false,
			// text of the input of a datalist, the options are only filtered by it once the user typed in the input
			query: "",
			typed: false,
			// text of the other inputs
			text: this.modelValue === null || this.modelValue === undefined || Array.isArray(this.modelValue) ? "" : String(this.modelValue),
			debouncedRefetchData: null as (() => void) | null,
		};
	},
	created() {
		this.debouncedRefetchData = debounce(this.refetchData, 500);
	},
	async mounted() {
		if (!this.isList) {
			return;
		}
		this.query = this.multiple ? "" : (this.selected[0]?.text ?? "");
		if (this.hasStore) {
			await this.refetchData();
		}
	},
	// the value is the id of the option, "update" gives the values chosen by the user (an array if multiple), converting them is up to the container
	emits: {
		update: (_value: FilterValue | FilterValue[]) => true,
	},
	computed: {
		inputId(): string {
			return `filter-input-${this.$.uid}`;
		},
		isList(): boolean {
			return this.type === "datalist" || this.type === "select";
		},
		// the options come from a store, the user can also type a text that is not an option
		hasStore(): boolean {
			return this.storeData !== undefined && this.storeKey !== undefined;
		},
		// every option of the datalist
		allOptions(): ListItem[] {
			if (this.hasStore) {
				const storeKey = this.storeKey ?? "";
				return Object.values(this.storeData ?? {}).map((element) => ({ value: element[storeKey], text: String(element[storeKey]) }));
			}
			return this.options.map((option) => ({ value: option.id, text: option.value }));
		},
		// options of the datalist that are currently selected
		selected(): ListItem[] {
			return filledValues({ value: this.modelValue }).map((value) => {
				const option = this.allOptions.find((o) => String(o.value) === String(value));
				return { value: value as ListItem["value"], text: option?.text ?? String(value) };
			});
		},
		// selected options displayed as removable chips, a single value is displayed as the text of the input
		chips(): ListItem[] {
			return this.multiple ? this.selected : [];
		},
		// options of the datalist that can be chosen
		listOptions(): ListItem[] {
			const search = this.typed ? toLowerCaseWithoutAccents(this.query) : "";
			const selectedValues = new Set(this.selected.map((item) => String(item.value)));
			const result = this.allOptions.filter((option) => {
				if (this.multiple && selectedValues.has(String(option.value))) {
					return false;
				}
				return toLowerCaseWithoutAccents(option.text).includes(search);
			});
			if (this.sortOptions) {
				const order = this.sortOptions === "desc" ? -1 : 1;
				result.sort((a, b) => order * toLowerCaseWithoutAccents(a.text).localeCompare(toLowerCaseWithoutAccents(b.text)));
			}
			return result;
		},
	},
	methods: {
		onTextInput(event: Event) {
			this.text = (event.target as HTMLInputElement).value;
			this.$emit("update", this.text);
		},
		onFocus() {
			this.isOpen = true;
			this.typed = false;
			// the whole list of options is displayed, the selected text comes back if the user leaves without typing
			this.query = "";
			window.addEventListener("scroll", this.updatePosition, true);
			window.addEventListener("resize", this.updatePosition);
			void nextTick(() => this.updatePosition());
		},
		onInput() {
			this.typed = true;
			this.isOpen = true;
			if (this.hasStore) {
				this.debouncedRefetchData?.();
			}
		},
		onBlur() {
			this.isOpen = false;
			window.removeEventListener("scroll", this.updatePosition, true);
			window.removeEventListener("resize", this.updatePosition);
			this.commitQuery();
		},
		onBackspace() {
			if (this.multiple && this.query === "" && this.selected.length > 0) {
				this.removeValue(this.selected[this.selected.length - 1].value);
			}
		},
		// what the user typed becomes the value of the filter when the input is left
		commitQuery() {
			const selectedText = this.multiple ? "" : (this.selected[0]?.text ?? "");
			if (!this.typed || this.multiple) {
				this.query = selectedText;
			} else if (this.hasStore) {
				// the text is the value, the table also finds what is not in the options
				this.$emit("update", this.query);
			} else {
				const typedText = toLowerCaseWithoutAccents(this.query);
				const match = this.allOptions.find((option) => toLowerCaseWithoutAccents(option.text) === typedText);
				if (match) {
					this.$emit("update", match.value);
					this.query = match.text;
				} else if (typedText.trim() === "") {
					this.$emit("update", "");
					this.query = "";
				} else {
					this.query = selectedText;
				}
			}
			this.typed = false;
		},
		selectOption(option: ListItem) {
			if (this.multiple) {
				this.$emit("update", [...this.selected.map((item) => item.value), option.value]);
				this.query = "";
				this.typed = false;
				return;
			}
			this.$emit("update", option.value);
			this.query = option.text;
			this.typed = false;
			this.isOpen = false;
			// the blur restores the text of the selected option, which is only the new one once the value came back through modelValue
			void nextTick(() => (this.$refs.filterInput as HTMLInputElement | undefined)?.blur());
		},
		removeValue(value: ListItem["value"]) {
			if (this.multiple) {
				this.$emit("update", this.selected.map((item) => item.value).filter((v) => String(v) !== String(value)));
			} else {
				this.$emit("update", "");
				this.query = "";
			}
		},
		updatePosition() {
			const box = this.$refs.filterBox as HTMLElement | undefined;
			const list = this.$refs.filterList as HTMLElement | undefined;
			if (!this.isOpen || !box || !list) {
				return;
			}
			const rect = box.getBoundingClientRect();
			list.style.top = `${rect.bottom + window.scrollY}px`;
			list.style.left = `${rect.left + window.scrollX}px`;
			list.style.width = `${rect.width}px`;
		},
		async refetchData() {
			const filter: RSQLFilter[] = [{
				key: this.storeKey ?? "",
				compareMethod: "=like=",
				value: this.multiple || !this.typed ? "" : this.query,
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
