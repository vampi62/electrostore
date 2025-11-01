<template>
	<div class="relative mb-6 w-full sm:w-[490px]">
		<Form :validation-schema="schema" v-slot="{ errors }" @submit.prevent="">
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
						<template v-else-if="field.type === 'password'">
							<div class="relative">
								<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" :type="showPassword ? 'text' : 'password'" v-model="storeData[field.key]"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="field?.condition && !evaluateCondition(field.condition)" />
								<button type="button" @mouseup="showPassword = false" @mousedown="showPassword = true"
									class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600 hover:text-gray-800">
									<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
									</svg>
								</button>
							</div>
							<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
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
		<!-- Animation de chargement -->
		<div v-if="storeData.loading" class="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center z-10">
			<div class="loading-spinner">
				<div class="spinner-ring"></div>
			</div>
		</div>
	</div>
</template>

<script>
import { Form, Field } from "vee-validate";
export default {
	name: "FormContainer",
	props: {
		schemaBuilder: {
			type: Function,
			required: true,
			// This function should return a Yup validation schema based on the form fields
		},
		labels: {
			type: Array,
			required: true,
			// This should be an array of field objects, each containing:
			// - key: string (the key in the storeData for the field)
			// - label: string (translation key for the label)
			// - type: string (input type, e.g., 'text', 'number', 'select', 'checkbox', 'password', 'textarea', 'computed', 'custom')
			// - model: any (for checkbox type, the boolean model value)
			// - condition: string (optional, a JavaScript expression to evaluate whether to enable the field)
			// - options: array (for select inputs, optional, required if type is 'select')
			// - rows: number (for textarea inputs, optional)
		},
		storeData: {
			type: Object,
			default: () => ({}),
			// This should be an object containing the data for the form fields
		},
		storeUser: {
			type: Object,
			default: () => ({}),
			// This should be an object containing the user session data
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
	data() {
		return {
			showPassword: false,
		};
	},
};
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

@keyframes spin {
	0% { transform: rotate(0deg); }
	100% { transform: rotate(360deg); }
}
</style>