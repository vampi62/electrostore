<template>
	<div class="relative">
		<Field
			:id="inputId"
			v-model="value"
			:name="field.key"
			:type="showPassword ? 'text' : 'password'"
			class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
			:class="{ 'border-red-500': error }"
			:disabled="disabled"
		/>
		<button
			type="button"
			class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600 hover:text-gray-800"
			:disabled="disabled"
			@mousedown="showPassword = true"
			@mouseup="showPassword = false"
		>
			<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
			</svg>
		</button>
	</div>
	<FormFieldError :error="error" />
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { Field } from "vee-validate";
import FormFieldError from "@/components/form/FormFieldError.vue";
import type { FormLabel } from "@/types/form";

const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: unknown] }>();

const showPassword = ref(false);

const value = computed({
	get: () => props.modelValue,
	set: (newValue: unknown) => emit("update:modelValue", newValue),
});
</script>
