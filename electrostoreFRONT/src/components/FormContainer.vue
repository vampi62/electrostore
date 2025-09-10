<template>
	<Form :validation-schema="schema" v-slot="{ errors }" @submit.prevent="" class="mb-6 w-full sm:w-[490px]">
		<div class="flex flex-col text-gray-700 space-y-2">
			<div v-for="field in labels" :key="field.key" class="flex flex-col sm:flex-row sm:items-start sm:space-x-2 w-full">
				<label class="font-semibold sm:min-w-[140px]" :for="`form-input-${this.$.uid}-${field.key}`">{{ $t(field.label) }}:</label>
				<div class="flex flex-col flex-1 w-full">
					<template v-if="field.type === 'checkbox'">
						<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" v-slot="{ is_checked_custom }">
							<input
								v-model="field.model"
								v-bind="is_checked_custom"
								type="checkbox"
								:checked="field.model"
								class="form-checkbox h-5 w-5 text-blue-600"
								:disabled="field?.condition && !evaluateCondition(field.condition)"
							/>
						</Field>
					</template>
					<template v-else-if="field.type === 'select'">
						<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="select" v-model="storeData[field.key]"
							class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
							:class="{ 'border-red-500': errors[field.key] }"
							:disabled="field?.condition && !evaluateCondition(field.condition)">
							<option v-for="option in field.options" :key="option[0]" :value="option[0]">{{ option[1] }}</option>
						</Field>
						<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
					</template>
					<template v-else-if="field.type === 'textarea'">
						<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="textarea" v-model="storeData[field.key]" :rows="field.rows || 3"
							class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
							:class="{ 'border-red-500': errors[field.key] }"
							:disabled="field?.condition && !evaluateCondition(field.condition)" />
						<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
					</template>
					<template v-else-if="field.type === 'computed'">
						<div class="flex space x-2">
							<span>{{ field.value }}</span>
						</div>
					</template>
					<template v-else-if="field.type === 'custom'">
						<slot :name="field.key"></slot>
					</template>
					<template v-else>
						<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" :type="field.type" v-model="storeData[field.key]"
							class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
							:class="{ 'border-red-500': errors[field.key] }"
							:disabled="field?.condition && !evaluateCondition(field.condition)" />
						<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
					</template>
				</div>
			</div>
		</div>
	</Form>
</template>

<script>
import { Form, Field } from "vee-validate";
export default {
	name: "FormContainer",
	props: {
		schemaBuilder: {
			type: Function,
			required: true,
		},
		labels: {
			type: Array,
			required: true,
		},
		storeData: {
			type: Object,
			default: () => ({}),
		},
		storeUser: {
			type: Object,
			default: () => ({}),
		},
	},
	computed: {
		schema() {
			return this.schemaBuilder(this.labels.find((field) => field.key === "check")?.model || false);
		},
	},
	components: {
		Form,
		Field,
	},
	methods: {
		evaluateCondition(condition) {
			try {
				return new Function(["session","edition","form"], `return ${condition}`)(this.storeUser, this.storeData, this.labels);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
	},
};
</script>