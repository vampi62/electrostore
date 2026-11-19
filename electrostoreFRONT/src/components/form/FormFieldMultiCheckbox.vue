<template>
	<div class="flex flex-col space-y-2">
		<!-- Hidden Field for the vee-validate validation -->
		<Field :id="inputId" :name="field.key" :model-value="modelValue" type="hidden" />

		<div v-if="selectedOptions.length > 0" class="flex flex-wrap gap-1 p-2 bg-gray-50 border border-gray-300 rounded min-h-[32px]">
			<span
				v-for="[index, label] in selectedOptions"
				:key="index"
				class="inline-flex items-center px-2 py-1 bg-regal-bleu-ciel-30 text-regal-noir-100 text-xs rounded-full"
			>
				{{ label }}
				<button
					type="button"
					class="ml-1.5 hover:text-regal-noir-100 focus:outline-none"
					:disabled="disabled"
					@click="toggleOption(index)"
				>
					<svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
						<path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
					</svg>
				</button>
			</span>
		</div>

		<div class="relative">
			<button
				:id="`dropdown-button-${inputId}`"
				ref="buttonRef"
				type="button"
				class="w-full border border-gray-300 rounded px-3 py-2 text-left bg-white focus:outline-none focus:ring focus:ring-regal-bleu-ciel-100 flex items-center justify-between"
				:class="{ 'border-red-500': error, 'bg-gray-100 cursor-not-allowed': disabled }"
				:disabled="disabled"
				@click="toggleDropdown"
			>
				<span class="text-gray-600 text-sm">
					{{ selectedOptions.length > 0 ? `${selectedOptions.length} sélectionné(s)` : 'Sélectionner...' }}
				</span>
				<svg class="w-5 h-5 text-gray-400 transition-transform" :class="{ 'rotate-180': isOpen }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
				</svg>
			</button>

			<Teleport to="body">
				<div
					v-show="isOpen"
					ref="menuRef"
					:style="menuStyle"
					class="fixed z-[9999] bg-white border border-gray-300 rounded shadow-lg max-h-60 overflow-y-auto"
				>
					<div v-for="[index, option, optionDisabled, hidden] in options" :key="index" v-show="!hidden">
						<label
							class="flex items-center px-3 py-2 hover:bg-gray-50 cursor-pointer"
							:class="{ 'opacity-50 cursor-not-allowed': optionDisabled }"
						>
							<input
								type="checkbox"
								class="form-checkbox h-4 w-4 text-regal-turquoise-70 flex-shrink-0 rounded"
								:checked="isSelected(index)"
								:disabled="optionDisabled"
								@change="toggleOption(index)"
							/>
							<span class="ml-2 text-sm flex-1">{{ option }}</span>
						</label>
					</div>
				</div>
			</Teleport>
		</div>
	</div>
	<FormFieldError :error="error" />
</template>

<script setup lang="ts">
import { computed, nextTick, ref } from "vue";
import { Field } from "vee-validate";
import FormFieldError from "@/components/form/FormFieldError.vue";
import { useFloatingPanel } from "@/composables/useFloatingPanel";
import { getSortedOptions } from "@/utils/formOptions";
import type { FormLabel } from "@/types/form";

const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: unknown[]] }>();

const buttonRef = ref<HTMLButtonElement | null>(null);
const menuRef = ref<HTMLElement | null>(null);
const { isOpen, style: menuStyle, toggle: toggleDropdown, updatePosition } = useFloatingPanel(buttonRef, menuRef);

const options = computed(() => getSortedOptions(props.field));
const selectedValues = computed<unknown[]>(() => (Array.isArray(props.modelValue) ? props.modelValue : []));
const selectedOptions = computed(() => options.value.filter(([index]) => selectedValues.value.includes(index)));

function isSelected(index: string): boolean {
	return selectedValues.value.includes(index);
}

async function toggleOption(index: string) {
	const next = isSelected(index)
		? selectedValues.value.filter((value) => value !== index)
		: [...selectedValues.value, index];
	emit("update:modelValue", next);

	// the chips can change the height of the block, so the button moves
	if (isOpen.value) {
		await nextTick();
		updatePosition();
	}
}
</script>
