<template>
	<div class="relative w-full sm:w-[560px]">
		<Form ref="formRef" v-slot="{ errors }" :validation-schema="schema" @submit.prevent="">
			<div class="flex flex-col text-gray-700 space-y-2">
				<div
					v-for="field in labelsShown"
					:key="field.key + field.label + field.text"
					class="flex flex-col sm:flex-col sm:items-start sm:space-x-2 w-full"
				>
					<!-- Display-only fields -->
					<FormFieldSection v-if="field.type === 'section'" :field="field" />
					<FormFieldFixed v-else-if="field.type === 'fixed'" :field="field" />
					<FormFieldReadonly v-else-if="field.type === 'readonly'" :field="field" :value="formData[field.key]" />

					<!-- Input fields -->
					<template v-else>
						<label class="font-semibold sm:min-w-[150px]" :for="getInputId(field)">
							{{ field.label ? $t(field.label) : field.text }}
						</label>
						<div class="flex flex-col flex-1 w-full relative">
							<FormFieldComputed v-if="field.type === 'computed'" :field="field" />
							<component
								:is="getFieldComponent(field)"
								v-else
								:field="field"
								:input-id="getInputId(field)"
								:disabled="isFieldDisabled(field)"
								:error="errors[field.key]"
								:model-value="formData[field.key]"
								@update:model-value="updateField(field.key, $event)"
							>
								<slot v-if="field.type === 'custom'" :name="field.key"></slot>
							</component>

							<div v-if="isFieldLoading(field)" class="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center">
								<div class="loading-spinner">
									<div class="spinner-ring-small"></div>
								</div>
							</div>
						</div>
					</template>
				</div>
			</div>
		</Form>
		<div v-if="storeEdition.loading" class="absolute w-full sm:w-[570px] inset-0 bg-white bg-opacity-75 flex items-center justify-center z-10">
			<div class="loading-spinner">
				<div class="spinner-ring"></div>
			</div>
		</div>
	</div>
</template>

<script lang="ts">
import { defineComponent, toValue } from "vue";
import type { Component, PropType } from "vue";
import { Form } from "vee-validate";
import type { StateTree } from "pinia";
import type { FormFunction, FormLabel, FormSchema } from "@/types/form";
import FormFieldSection from "@/components/form/FormFieldSection.vue";
import FormFieldFixed from "@/components/form/FormFieldFixed.vue";
import FormFieldReadonly from "@/components/form/FormFieldReadonly.vue";
import FormFieldComputed from "@/components/form/FormFieldComputed.vue";
import FormFieldInput from "@/components/form/FormFieldInput.vue";
import FormFieldCheckbox from "@/components/form/FormFieldCheckbox.vue";
import FormFieldMultiCheckbox from "@/components/form/FormFieldMultiCheckbox.vue";
import FormFieldTable from "@/components/form/FormFieldTable.vue";
import FormFieldSelect from "@/components/form/FormFieldSelect.vue";
import FormFieldFetchSelect from "@/components/form/FormFieldFetchSelect.vue";
import FormFieldTextarea from "@/components/form/FormFieldTextarea.vue";
import FormFieldCustom from "@/components/form/FormFieldCustom.vue";
import FormFieldPassword from "@/components/form/FormFieldPassword.vue";

// field type -> component, any other type is a classic <input :type="field.type">
const fieldComponents: Record<string, Component | undefined> = {
	"checkbox": FormFieldCheckbox,
	"multi-checkbox": FormFieldMultiCheckbox,
	"table": FormFieldTable,
	"select": FormFieldSelect,
	"fetch-select": FormFieldFetchSelect,
	"textarea": FormFieldTextarea,
	"custom": FormFieldCustom,
	"password": FormFieldPassword,
};

export default defineComponent({
	name: "FormContainer",
	components: {
		Form,
		FormFieldSection,
		FormFieldFixed,
		FormFieldReadonly,
		FormFieldComputed,
	},
	props: {
		schemaBuilder: {
			type: Function as PropType<() => FormSchema>,
			required: true,
			// This function should return a Yup validation schema based on the form fields
		},
		labels: {
			type: Array as PropType<FormLabel[]>,
			required: true,
		},
		storeEdition: {
			type: Object as PropType<StateTree>,
			default: () => ({}),
			// This should be the edition of the element, the fields are edited in it
		},
		storeData: {
			type: Object as PropType<StateTree>,
			default: () => ({}),
			// This should be the element in its store, a field that is not in the edition is displayed from it (read-only fields)
		},
		storeUser: {
			type: Object as PropType<StateTree>,
			default: () => ({}),
			// This should be an object containing the user session data
		},
		storeFunction: {
			type: Object as PropType<FormFunction>,
			default: () => ({}),
			// This should be an object containing any helper functions that might be needed in conditions
		},
		permission: {
			type: Boolean,
			default: true,
		},
	},
	computed: {
		// the edition completed by the element: a field that is not in the edition is read from the element, the changes are always written in the edition
		formData(): StateTree {
			const data = this.storeData;
			return new Proxy(this.storeEdition, {
				get: (edition, key) => (key in edition ? edition[key] : data[key]),
				has: (edition, key) => key in edition || key in data,
				set: (edition, key, value) => {
					edition[key] = value;
					return true;
				},
				deleteProperty: (edition, key) => delete edition[key],
			});
		},
		schema(): FormSchema {
			return this.schemaBuilder();
		},
		labelsShown(): FormLabel[] {
			return this.labels.filter((field) => !field?.showCondition || this.evaluateCondition(field.showCondition));
		},
	},
	methods: {
		async validate(): Promise<{ valid: boolean; errors: Partial<Record<string, string>> }> {
			const form = this.$refs.formRef as InstanceType<typeof Form> | undefined;
			if (!form) {
				return { valid: false, errors: {} };
			}
			return await form.validate();
		},
		evaluateCondition(condition: string): boolean {
			try {
				return new Function("session", "edition", "form", "func", `return ${condition}`)(this.storeUser, this.formData, this.labels, this.storeFunction);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		isFieldLoading(field: FormLabel): boolean {
			return !!toValue(field.loading);
		},
		isFieldDisabled(field: FormLabel): boolean {
			return !this.permission
				|| (!!field.enableCondition && !this.evaluateCondition(field.enableCondition))
				|| this.isFieldLoading(field);
		},
		getInputId(field: FormLabel): string {
			return `form-input-${this.$.uid}-${field.key}`;
		},
		getFieldComponent(field: FormLabel): Component {
			return fieldComponents[field.type ?? ""] ?? FormFieldInput;
		},
		updateField(key: string, value: unknown) {
			this.formData[key] = value;
		},
	},
});
</script>

<style scoped>
.loading-spinner {
	display: flex;
	align-items: center;
	justify-content: center;
}

.spinner-ring {
	width: 40px;
	height: 40px;
	border: 3px solid #f3f3f3;
	border-top: 3px solid #3b82f6;
	border-radius: 50%;
	animation: spin 1s linear infinite;
}

.spinner-ring-small {
	width: 20px;
	height: 20px;
	border: 2px solid #f3f3f3;
	border-top: 2px solid #3b82f6;
	border-radius: 50%;
	animation: spin 1s linear infinite;
}

@keyframes spin {
	0% { transform: rotate(0deg); }
	100% { transform: rotate(360deg); }
}
</style>
