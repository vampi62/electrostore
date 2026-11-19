<template>
	<Field v-model="value" :name="field.key" type="hidden" />
	<div class="relative">
		<input
			:id="inputId"
			ref="inputRef"
			type="text"
			:value="displayText"
			class="border border-gray-300 rounded px-2 py-1 w-full pr-7 focus:outline-none focus:ring focus:ring-blue-300"
			:class="{ 'border-red-500': error }"
			:placeholder="field.placeholder ? $t(field.placeholder) : ''"
			:disabled="disabled"
			@input="onInput"
			@focus="onFocus"
			@blur="close"
		/>
		<div class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
			<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
			</svg>
		</div>
	</div>

	<Teleport to="body">
		<div
			v-show="isOpen"
			ref="menuRef"
			:style="menuStyle"
			class="fixed z-[9999] bg-white border border-gray-300 rounded shadow-lg max-h-60 overflow-y-auto"
		>
			<div
				v-for="[optionValue, label] in options"
				:key="optionValue"
				class="px-3 py-2 hover:bg-gray-50 cursor-pointer text-sm"
				@mousedown.prevent="selectOption(optionValue, label)"
			>
				{{ label }}
			</div>
			<div v-if="options.length === 0" class="px-3 py-2 text-sm text-gray-400 italic">
				Aucun résultat
			</div>
		</div>
	</Teleport>
	<FormFieldError :error="error" />
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref } from "vue";
import { Field } from "vee-validate";
import { debounce } from "lodash-es";
import FormFieldError from "@/components/form/FormFieldError.vue";
import { useFloatingPanel } from "@/composables/useFloatingPanel";
import type { FormLabel } from "@/types/form";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: unknown] }>();

const inputRef = ref<HTMLInputElement | null>(null);
const menuRef = ref<HTMLElement | null>(null);
const { isOpen, style: menuStyle, open, close, updatePosition } = useFloatingPanel(inputRef, menuRef);

// text typed by the user, used to search the options
const inputText = ref("");

const value = computed({
	get: () => props.modelValue,
	set: (newValue: unknown) => emit("update:modelValue", newValue),
});

const valueKey = computed(() => props.field.fetchValueKey || (props.field.fetchStoreKey ?? ""));

// label of the selected element
const selectedLabel = computed<string>(() => {
	const { fetchStore, fetchStoreKey = "" } = props.field;
	if (!fetchStore) {
		return "";
	}
	const found = Object.values(fetchStore).find((item) => String(item[valueKey.value]) === String(props.modelValue));
	return found?.[fetchStoreKey] || "";
});

// while the list is open the input is a search field, otherwise it shows the selection
const displayText = computed(() => (isOpen.value ? inputText.value : selectedLabel.value));

const options = computed<[string | number, string][]>(() => {
	const { fetchStore, fetchStoreKey = "" } = props.field;
	if (!fetchStore) {
		return [];
	}
	const search = inputText.value.toLowerCase();
	return Object.values(fetchStore)
		.filter((item) => String(item[fetchStoreKey]).toLowerCase().includes(search))
		.map((item): [string | number, string] => [item[valueKey.value], item[fetchStoreKey]]);
});

async function search() {
	const { fetchFunction, fetchStoreKey = "" } = props.field;
	if (!fetchFunction) {
		return;
	}
	const filter: RSQLFilter[] = [{ key: fetchStoreKey, compareMethod: "=like=", value: inputText.value }];
	const sort: RSQLSort = { key: fetchStoreKey, order: "asc" };
	await fetchFunction(10, 0, [], filter, sort, false);
}

const debouncedSearch = debounce(async() => {
	await search();
	await nextTick();
	updatePosition();
}, 300);

onBeforeUnmount(() => debouncedSearch.cancel());

async function onFocus() {
	inputText.value = "";
	await open();
	await search();
}

function onInput(event: Event) {
	inputText.value = (event.target as HTMLInputElement).value;
	void open();
	debouncedSearch();
}

function selectOption(optionValue: string | number, label: string) {
	emit("update:modelValue", optionValue);
	inputText.value = label;
	close();
	void nextTick(() => inputRef.value?.blur());
}
</script>
