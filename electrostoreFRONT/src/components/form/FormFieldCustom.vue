<template>
	<Field :id="inputId" v-model="value" :name="field.key" type="hidden" />
	<slot />
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
