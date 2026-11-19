<template>
	<Field
		:id="inputId"
		:name="field.key"
		as="select"
		:model-value="modelValue"
		class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
		:class="{ 'border-red-500': error }"
		:disabled="disabled"
		@update:model-value="onUpdate"
	>
		<template v-for="[index, option, optionDisabled, hidden] in options" :key="index">
			<option :value="index" :disabled="optionDisabled" v-show="!hidden">{{ option }}</option>
		</template>
	</Field>
	<FormFieldError :error="error" />
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Field } from "vee-validate";
import FormFieldError from "@/components/form/FormFieldError.vue";
import { getSortedOptions } from "@/utils/formOptions";
import type { FormLabel } from "@/types/form";

const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: unknown] }>();

const options = computed(() => getSortedOptions(props.field));

function onUpdate(raw: unknown) {
	if (props.field.typeData === "number") {
		const parsed = parseFloat(String(raw));
		emit("update:modelValue", Number.isNaN(parsed) ? raw : parsed);
	} else if (props.field.typeData === "bool") {
		emit("update:modelValue", raw === "true" || raw === true);
	} else {
		emit("update:modelValue", raw);
	}
}
</script>
