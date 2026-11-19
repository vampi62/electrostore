<template>
	<Field :id="inputId" :name="field.key">
		<input v-model="checked" type="checkbox" class="form-checkbox h-5 w-5 text-blue-600" :disabled="disabled" />
	</Field>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Field } from "vee-validate";
import type { FormLabel } from "@/types/form";

// `error` is part of the common contract of the fields, unused here
const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: boolean] }>();

const checked = computed({
	get: () => Boolean(props.modelValue),
	set: (newValue: boolean) => emit("update:modelValue", newValue),
});
</script>
