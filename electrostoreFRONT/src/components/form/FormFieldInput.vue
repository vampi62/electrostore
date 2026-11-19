<template>
	<Field
		:id="inputId"
		v-model="value"
		:name="field.key"
		:type="field.type"
		class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
		:class="{ 'border-red-500': error }"
		:placeholder="field.placeholder ? $t(field.placeholder) : ''"
		:disabled="disabled"
	/>
	<FormFieldError :error="error" />
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Field } from "vee-validate";
import FormFieldError from "@/components/form/FormFieldError.vue";
import type { FormLabel } from "@/types/form";

const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: unknown] }>();

const value = computed({
	get: () => props.modelValue,
	set: (newValue: unknown) => emit("update:modelValue", newValue),
});
</script>
