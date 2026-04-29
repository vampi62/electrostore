<template>
	<div class="relative w-full sm:w-[560px]">
		<Form ref="formRef" :validation-schema="schema" v-slot="{ errors }" @submit.prevent="">
			<div class="flex flex-col text-gray-700 space-y-2">
				<div v-for="field in labelsShown" :key="field.key + field.label + field.text" class="flex flex-col sm:flex-col sm:items-start sm:space-x-2 w-full">
					<template v-if="field.type === 'section'">
						<h2 class="text-lg font-bold w-full pt-4">{{ $t(field.label) }}</h2>
						<hr class="w-full border-gray-300 mb-2" />
						<div class="w-full"></div>
						<!-- Utiliser une div vide pour forcer la ligne suivante à prendre toute la largeur -->
						<!-- Les champs suivants seront sur une nouvelle ligne -->
					</template>
					<template v-else-if="field.type === 'fixed'">
						<span v-if="field.label" class="font-semibold sm:min-w-[150px]" :for="`form-input-${this.$.uid}-${field.key}`">{{ $t(field.label) }}</span>
						<span v-else class="font-semibold sm:min-w-[150px]" :for="`form-input-${this.$.uid}-${field.key}`">{{ field.text }}</span>
					</template>
					<template v-else>
						<label v-if="field.label" class="font-semibold sm:min-w-[150px]" :for="`form-input-${this.$.uid}-${field.key}`">{{ $t(field.label) }}</label>
						<label v-else class="font-semibold sm:min-w-[150px]" :for="`form-input-${this.$.uid}-${field.key}`">{{ field.text }}</label>
						<div class="flex flex-col flex-1 w-full relative">
							<template v-if="field.type === 'checkbox'">
								<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" v-slot="{ is_checked_custom }">
									<input
										v-model="storeData[field.key]"
										v-bind="is_checked_custom"
										type="checkbox"
										:value="storeData[field.key]"
										class="form-checkbox h-5 w-5 text-blue-600"
										:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading"
									/>
								</Field>
							</template>
							<template v-else-if="field.type === 'multi-checkbox'">
								<div class="flex flex-col space-y-2">
									<!-- Field caché pour la validation vee-validate -->
									<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" v-model="storeData[field.key]" type="hidden" />
									<div v-if="getSelectedOptions(field).length > 0" class="flex flex-wrap gap-1 p-2 bg-gray-50 border border-gray-300 rounded min-h-[32px]">
										<span 
											v-for="[index, label] in getSelectedOptions(field)" 
											:key="index"
											class="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full"
										>
											{{ label }}
											<button
												type="button"
												@click="removeSelection(field.key, index)"
												class="ml-1.5 text-blue-600 hover:text-blue-800 focus:outline-none"
												:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading"
											>
												<svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
													<path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
												</svg>
											</button>
										</span>
									</div>
									<div class="relative">
										<button
											type="button"
											@click="toggleDropdown(field.key, $event)"
											:ref="`dropdown-button-${field.key}`"
											:id="`dropdown-button-${this.$.uid}-${field.key}`"
											class="w-full border border-gray-300 rounded px-3 py-2 text-left bg-white focus:outline-none focus:ring focus:ring-blue-300 flex items-center justify-between"
											:class="{ 'border-red-500': errors[field.key], 'bg-gray-100 cursor-not-allowed': (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading }"
											:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading"
										>
											<span class="text-gray-600 text-sm">
												{{ getSelectedOptions(field).length > 0 ? `${getSelectedOptions(field).length} sélectionné(s)` : 'Sélectionner...' }}
											</span>
											<svg class="w-5 h-5 text-gray-400 transition-transform" :class="{ 'rotate-180': dropdownOpen[field.key] }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
											</svg>
										</button>
										<Teleport to="body">
											<div
												v-show="dropdownOpen[field.key]"
												:ref="`dropdown-menu-${field.key}`"
												:style="getDropdownStyle(field.key)"
												class="fixed z-[9999] bg-white border border-gray-300 rounded shadow-lg max-h-60 overflow-y-auto"
											>
												<div v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index" v-show="!hidden">
													<label 
														class="flex items-center px-3 py-2 hover:bg-gray-50 cursor-pointer"
														:class="{ 'opacity-50 cursor-not-allowed': disabled }"
													>
														<input
															v-model="storeData[field.key]"
															type="checkbox"
															:value="index"
															@focus="ensureArray(field.key)"
															@change="recalculateDropdownPosition(field.key)"
															class="form-checkbox h-4 w-4 text-blue-600 flex-shrink-0 rounded"
															:disabled="disabled"
														/>
														<span class="ml-2 text-sm flex-1">{{ option }}</span>
													</label>
												</div>
											</div>
										</Teleport>
									</div>
								</div>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'select'">
								<Field v-if="field?.typeData === 'number'" :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="select" v-model.number="storeData[field.key]"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<Field v-else-if="field?.typeData === 'bool'" :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="select"
									:model-value="storeData[field.key]"
									@update:model-value="storeData[field.key] = $event === 'true' || $event === true"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<Field v-else :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="select" v-model="storeData[field.key]"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'textarea'">
								<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" as="textarea" v-model="storeData[field.key]" :rows="field.rows || 3"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:placeholder="field.placeholder ? $t(field.placeholder) : ''"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading" />
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'computed'">
								<div class="flex space x-2">
									<span>{{ field.value }}</span>
								</div>
							</template>
							<template v-else-if="field.type === 'custom'">
								<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" v-model="storeData[field.key]" type="hidden" />
								<slot :name="field.key"></slot>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'password'">
								<div class="relative">
									<Field :id="`form-input-${this.$.uid}-${field.key}`" :name="field.key" :type="showPassword ? 'text' : 'password'" v-model="storeData[field.key]"
										class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
										:class="{ 'border-red-500': errors[field.key] }"
										:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading" />
									<button type="button" @mouseup="showPassword = false" @mousedown="showPassword = true"
										class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600 hover:text-gray-800"
										:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
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
									:placeholder="field.placeholder ? $t(field.placeholder) : ''"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading" />
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<div v-if="field?.loading" class="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center">
								<div class="loading-spinner">
									<div class="spinner-ring-small"></div>
								</div>
							</div>
						</div>
					</template>
				</div>
			</div>
		</Form>
		<div v-if="storeData.loading" class="absolute w-full sm:w-[570px] inset-0 bg-white bg-opacity-75 flex items-center justify-center z-10">
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
			// - enableCondition: string (optional, a JavaScript expression to evaluate whether to enable the field)
			// - showCondition: string (optional, a JavaScript expression to evaluate whether to show the field)
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
		storeFunction: {
			type: Object,
			default: () => ({}),
			// This should be an object containing any helper functions that might be needed in conditions
		},
		permission: {
			type: Boolean,
			default: true,
		},
	},
	computed: {
		schema() {
			return this.schemaBuilder();
		},
		labelsShown() {
			return this.labels.filter((field) => !field?.showCondition || this.evaluateCondition(field.showCondition));
		},
	},
	components: {
		Form,
		Field,
	},
	methods: {
		async validate() {
			if (!this.$refs.formRef) {
				return { valid: false, errors: {} };
			}
			const result = await this.$refs.formRef.validate();
			return result;
		},
		evaluateCondition(condition) {
			try {
				return new Function(["session","edition","form","func"], `return ${condition}`)(this.storeUser, this.storeData, this.labels, this.storeFunction);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		ensureArray(key) {
			if (!Array.isArray(this.storeData[key])) {
				this.storeData[key] = [];
			}
			return this.storeData[key];
		},
		toggleMultiCheckbox(key, value) {
			if (!Array.isArray(this.storeData[key])) {
				this.storeData[key] = [];
			}
			
			const index = this.storeData[key].indexOf(value);
			if (index > -1) {
				this.storeData[key].splice(index, 1);
			} else {
				this.storeData[key].push(value);
			}
		},
		toggleDropdown(key, event) {
			this.dropdownOpen[key] = !this.dropdownOpen[key];
			if (this.dropdownOpen[key] && event) {
				this.$nextTick(() => {
					this.updateDropdownPosition(key, event.currentTarget);
				});
			}
		},
		closeDropdown(key) {
			this.dropdownOpen[key] = false;
		},
		updateDropdownPosition(key, button) {
			if (!button) {
				return;
			}
			
			const rect = button.getBoundingClientRect();
			this.dropdownPositions[key] = {
				top: rect.bottom + window.scrollY,
				left: rect.left + window.scrollX,
				width: rect.width,
			};
		},
		getDropdownStyle(key) {
			const pos = this.dropdownPositions[key];
			if (!pos) {
				return {};
			}
			
			return {
				top: `${pos.top + 4}px`,
				left: `${pos.left}px`,
				width: `${pos.width}px`,
			};
		},
		handleScrollResize() {
			// Mettre à jour les positions de tous les dropdowns ouverts
			Object.keys(this.dropdownOpen).forEach((key) => {
				if (this.dropdownOpen[key]) {
					const buttonRef = `dropdown-button-${key}`;
					const button = this.$refs[buttonRef];
					if (button) {
						this.updateDropdownPosition(key, Array.isArray(button) ? button[0] : button);
					}
				}
			});
		},
		handleClickOutside(event) {
			// Fermer les dropdowns si on clique en dehors
			Object.keys(this.dropdownOpen).forEach((key) => {
				if (this.dropdownOpen[key]) {
					const buttonRef = `dropdown-button-${key}`;
					const menuRef = `dropdown-menu-${key}`;
					const button = this.$refs[buttonRef];
					const menu = this.$refs[menuRef];
					
					const buttonEl = Array.isArray(button) ? button[0] : button;
					const menuEl = Array.isArray(menu) ? menu[0] : menu;
					
					const clickedInsideButton = buttonEl && buttonEl.contains(event.target);
					const clickedInsideMenu = menuEl && menuEl.contains(event.target);
					
					if (!clickedInsideButton && !clickedInsideMenu) {
						this.closeDropdown(key);
					}
				}
			});
		},
		getSelectedOptions(field) {
			if (!this.storeData[field.key] || !Array.isArray(this.storeData[field.key])) {
				return [];
			}
			
			const selectedValues = this.storeData[field.key];
			return this.getSortedOptions(field)
				.filter(([index]) => selectedValues.includes(index))
				.map(([index, label]) => [index, label]);
		},
		removeSelection(key, value) {
			if (!Array.isArray(this.storeData[key])) {
				return;
			}
			
			const index = this.storeData[key].indexOf(value);
			if (index > -1) {
				this.storeData[key].splice(index, 1);
				// Recalculer la position après le retrait (attendre que le DOM soit mis à jour)
				this.recalculateDropdownPosition(key);
			}
		},
		recalculateDropdownPosition(key) {
			// Attendre que le DOM soit mis à jour avec le nouvel état
			this.$nextTick(() => {
				if (this.dropdownOpen[key]) {
					const buttonRef = `dropdown-button-${key}`;
					const button = this.$refs[buttonRef];
					if (button) {
						this.updateDropdownPosition(key, Array.isArray(button) ? button[0] : button);
					}
				}
			});
		},
		getSortedOptions(field) {
			if (!field.options) {
				return [];
			}
			
			const entries = Object.entries(field.options).map(([key, value]) => {
				// Supporter les deux formats
				if (typeof value === "object" && value !== null) {
					return [key, value.label || "", value.disabled || false, value.hidden || false];
				}
				return [key, value, false, false]; // [key, label, disabled, hidden]
			});
			
			if (!field?.sort) {
				return entries;
			}
			
			return entries.sort((a, b) => {
				const valueA = a[1]; // label
				const valueB = b[1]; // label
				
				if (field.sort === "asc") {
					return String(valueA).localeCompare(String(valueB));
				} else if (field.sort === "desc") {
					return String(valueB).localeCompare(String(valueA));
				}
				
				return 0;
			});
		},
	},
	data() {
		return {
			showPassword: false,
			dropdownOpen: {},
			dropdownPositions: {},
		};
	},
	mounted() {
		window.addEventListener("scroll", this.handleScrollResize, true);
		window.addEventListener("resize", this.handleScrollResize);
		document.addEventListener("click", this.handleClickOutside);
	},
	unmounted() {
		window.removeEventListener("scroll", this.handleScrollResize, true);
		window.removeEventListener("resize", this.handleScrollResize);
		document.removeEventListener("click", this.handleClickOutside);
	},
	directives: {},
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