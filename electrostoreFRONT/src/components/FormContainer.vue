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
						<span v-if="field.label" class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ $t(field.label) }}</span>
						<span v-else class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ field.text }}</span>
					</template>
					<template v-else-if="field.type === 'readonly'">
						<span v-if="field.label" class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ $t(field.label) }}</span>
						<span v-else class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ storeData[field.key] }}</span>
					</template>
					<template v-else>
						<label v-if="field.label" class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ $t(field.label) }}</label>
						<label v-else class="font-semibold sm:min-w-[150px]" :for="`form-input-${$.uid}-${field.key}`">{{ field.text }}</label>
						<div class="flex flex-col flex-1 w-full relative">
							<template v-if="field.type === 'checkbox'">
								<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" v-slot="{ is_checked_custom }: any">
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
									<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" v-model="storeData[field.key]" type="hidden" />
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
											:id="`dropdown-button-${$.uid}-${field.key}`"
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
								<Field v-if="field?.typeData === 'number'" :id="`form-input-${$.uid}-${field.key}`" :name="field.key" as="select" v-model.number="storeData[field.key]"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<Field v-else-if="field?.typeData === 'bool'" :id="`form-input-${$.uid}-${field.key}`" :name="field.key" as="select"
									:model-value="storeData[field.key]"
									@update:model-value="storeData[field.key] = $event === 'true' || $event === true"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<Field v-else :id="`form-input-${$.uid}-${field.key}`" :name="field.key" as="select" v-model="storeData[field.key]"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors[field.key] }"
									:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading">
									<template v-for="[index, option, disabled, hidden] in getSortedOptions(field)" :key="index">
										<option :value="index" :disabled="disabled" v-show="!hidden">{{ option }}</option>
									</template>
								</Field>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'fetch-select'">
								<Field :name="field.key" v-model="storeData[field.key]" type="hidden" />
								<div class="relative">
									<input
										:id="`form-input-${$.uid}-${field.key}`"
										:ref="`fetch-select-input-${field.key}`"
										type="text"
										:value="getFetchSelectInputText(field)"
										@input="handleFetchSelectInput(field, $event)"
										@focus="openFetchSelect(field, $event)"
										@blur="closeFetchSelect(field.key)"
										class="border border-gray-300 rounded px-2 py-1 w-full pr-7 focus:outline-none focus:ring focus:ring-blue-300"
										:class="{ 'border-red-500': errors[field.key] }"
										:placeholder="field.placeholder ? $t(field.placeholder) : ''"
										:disabled="(!permission) || (field?.enableCondition && !evaluateCondition(field.enableCondition)) || field?.loading"
									/>
									<div class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
										<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
										</svg>
									</div>
								</div>
								<Teleport to="body">
									<div
										v-show="isFetchSelectOpen(field.key)"
										:ref="`fetch-select-menu-${field.key}`"
										:style="getFetchSelectStyle(field.key)"
										class="fixed z-[9999] bg-white border border-gray-300 rounded shadow-lg max-h-60 overflow-y-auto"
									>
										<div
											v-for="[value, label] in getFetchSelectOptions(field)"
											:key="value"
											@mousedown.prevent="selectFetchOption(field, value, label)"
											class="px-3 py-2 hover:bg-gray-50 cursor-pointer text-sm"
										>
											{{ label }}
										</div>
										<div v-if="getFetchSelectOptions(field).length === 0" class="px-3 py-2 text-sm text-gray-400 italic">
											Aucun résultat
										</div>
									</div>
								</Teleport>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'textarea'">
								<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" as="textarea" v-model="storeData[field.key]" :rows="field.rows || 3"
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
								<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" v-model="storeData[field.key]" type="hidden" />
								<slot :name="field.key"></slot>
								<span class="text-red-500 h-5 w-full text-sm">{{ errors[field.key] || ' ' }}</span>
							</template>
							<template v-else-if="field.type === 'password'">
								<div class="relative">
									<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" :type="showPassword ? 'text' : 'password'" v-model="storeData[field.key]"
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
								<Field :id="`form-input-${$.uid}-${field.key}`" :name="field.key" :type="field.type" v-model="storeData[field.key]"
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

<script lang="ts">
import { Form, Field } from "vee-validate";
import type { PropType } from "vue";
import { debounce } from "lodash-es";
import type { FormLabel } from "@/types/form";
export default {
	name: "FormContainer",
	props: {
		schemaBuilder: {
			type: Function,
			required: true,
			// This function should return a Yup validation schema based on the form fields
		},
		labels: {
			type: Array as PropType<FormLabel[]>,
			required: true,
		},
		storeData: {
			type: Object as PropType<Record<string, any>>,
			default: () => ({}),
			// This should be an object containing the data for the form fields
		},
		storeUser: {
			type: Object as PropType<Record<string, any>>,
			default: () => ({}),
			// This should be an object containing the user session data
		},
		storeFunction: {
			type: Object as PropType<Record<string, any>>,
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
	created() {
		this.fetchSelectDebouncers = {};
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
			const result = await (this.$refs.formRef as any).validate();
			return result;
		},
		evaluateCondition(condition: string): boolean {
			try {
				return new Function("session", "edition", "form", "func", `return ${condition}`)(this.storeUser, this.storeData, this.labels, this.storeFunction);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		ensureArray(key: string): any[] {
			if (!Array.isArray(this.storeData[key])) {
				this.storeData[key] = [];
			}
			return this.storeData[key];
		},
		toggleMultiCheckbox(key: string, value: any) {
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
		toggleDropdown(key: string, event?: Event) {
			this.dropdownOpen[key] = !this.dropdownOpen[key];
			if (this.dropdownOpen[key] && event && event.currentTarget instanceof HTMLElement) {
				const target = event.currentTarget;
				this.$nextTick(() => {
					this.updateDropdownPosition(key, target);
				});
			}
		},
		closeDropdown(key: string) {
			this.dropdownOpen[key] = false;
		},
		updateDropdownPosition(key: string, button: HTMLElement) {
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
		recalculateDropdownPosition(key: any) {
			const buttonRef = `dropdown-button-${key}`;
			const button = this.$refs[buttonRef];
			if (button) {
				this.updateDropdownPosition(key, Array.isArray(button) ? button[0] : button);
			}
		},
		getDropdownStyle(key: string) {
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
			// update the positions of all open dropdowns
			for (const key of Object.keys(this.dropdownOpen)) {
				if (this.dropdownOpen[key]) {
					const buttonRef = `dropdown-button-${key}`;
					const button = this.$refs[buttonRef];
					if (button) {
						this.updateDropdownPosition(key, Array.isArray(button) ? button[0] : button);
					}
				}
			}
			// update the positions of all open fetch-selects
			for (const key of Object.keys(this.fetchSelectState)) {
				if (this.fetchSelectState[key]?.isOpen) {
					const inputRef = `fetch-select-input-${key}`;
					const input = this.$refs[inputRef];
					if (input) {
						this.updateFetchSelectPosition(key, Array.isArray(input) ? input[0] : input);
					}
				}
			}
		},
		handleClickOutside(event: MouseEvent) {
			// Close dropdowns if clicking outside
			for (const key of Object.keys(this.dropdownOpen)) {
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
			}
		},
		getSelectedOptions(field: any) {
			if (!this.storeData[field.key] || !Array.isArray(this.storeData[field.key])) {
				return [];
			}
			
			const selectedValues = this.storeData[field.key];
			return this.getSortedOptions(field)
				.filter(([index]) => selectedValues.includes(index))
				.map(([index, label]) => [index, label]);
		},
		removeSelection(key: string, value: any) {
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
		initFetchSelectState(field: FormLabel) {
			const key = field.key;
			if (!this.fetchSelectState[key]) {
				let initialText = "";
				if (this.storeData[key] !== undefined && this.storeData[key] !== null && this.storeData[key] !== "" && field.fetchStore) {
					const fetchValueKey = field.fetchValueKey || (field.fetchStoreKey ?? "");
					const found = Object.values(field.fetchStore).find((item: Record<string, any>) => String(item[fetchValueKey]) === String(this.storeData[key]));
					if (found) {
						initialText = found[field.fetchStoreKey ?? ""];
					}
				}
				this.fetchSelectState[key] = { inputText: initialText, isOpen: false, position: null };
			}
		},
		getFetchSelectInputText(field: FormLabel): string {
			if (!field.fetchStore) {
				return "";
			}
			const fetchValueKey = field.fetchValueKey || (field.fetchStoreKey ?? "");
			const found = Object.values(field.fetchStore).find((item: Record<string, any>) => String(item[fetchValueKey]) === String(this.storeData[field.key]));
			return found?.[field.fetchStoreKey ?? ""] || "";
		},
		isFetchSelectOpen(key: string): boolean {
			return this.fetchSelectState[key]?.isOpen || false;
		},
		getFetchSelectStyle(key: string): Record<string, string> {
			const pos = this.fetchSelectState[key]?.position;
			if (!pos) {
				return {};
			}
			return {
				top: `${pos.top + 4}px`,
				left: `${pos.left}px`,
				width: `${pos.width}px`,
			};
		},
		updateFetchSelectPosition(key: string, inputEl?: Element | null) {
			if (!inputEl) {
				return;
			}
			const rect = inputEl.getBoundingClientRect();
			this.fetchSelectState[key].position = {
				top: rect.bottom + window.scrollY,
				left: rect.left + window.scrollX,
				width: rect.width,
			};
		},
		async openFetchSelect(field: FormLabel, event: Event) {
			this.initFetchSelectState(field);
			this.fetchSelectState[field.key].isOpen = true;
			this.fetchSelectState[field.key].inputText = "";
			await this.doFetchSelectSearch(field);
			this.$nextTick(() => {
				if (event.target instanceof Element) {
					this.updateFetchSelectPosition(field.key, event.target);
				}
			});
		},
		closeFetchSelect(key: string) {
			if (this.fetchSelectState[key]) {
				this.fetchSelectState[key].isOpen = false;
			}
		},
		selectFetchOption(field: FormLabel, value: any, label: string) {
			this.storeData[field.key] = value;
			this.fetchSelectState[field.key].inputText = label;
			this.fetchSelectState[field.key].isOpen = false;
			this.$nextTick(() => {
				const input = this.$refs[`fetch-select-input-${field.key}`] as any;
				if (input) {
					(Array.isArray(input) ? input[0] : input).blur();
				}
			});
		},
		handleFetchSelectInput(field: FormLabel, event: Event) {
			this.initFetchSelectState(field);
			const target = event.target as HTMLInputElement;
			this.fetchSelectState[field.key].inputText = target.value;
			this.fetchSelectState[field.key].isOpen = true;
			if (!this.fetchSelectDebouncers[field.key]) {
				this.fetchSelectDebouncers[field.key] = debounce(async(f) => {
					await this.doFetchSelectSearch(f);
					this.$nextTick(() => {
						const inputRef = `fetch-select-input-${f.key}`;
						const input = this.$refs[inputRef];
						if (input) {
							this.updateFetchSelectPosition(f.key, Array.isArray(input) ? input[0] : input);
						}
					});
				}, 300);
			}
			this.fetchSelectDebouncers[field.key](field);
		},
		async doFetchSelectSearch(field: FormLabel) {
			if (!field.fetchFunction) {
				return;
			}
			const inputText = this.fetchSelectState[field.key]?.inputText || "";
			const filter = [{ key: field.fetchStoreKey ?? "", compareMethod: "=like=", value: inputText }];
			const sort = { key: field.fetchStoreKey ?? "", order: "asc" as const };
			await field.fetchFunction(10, 0, [], filter, sort, false);
		},
		getFetchSelectOptions(field: FormLabel) {
			if (!field.fetchStore) {
				return [];
			}
			const fetchValueKey = field.fetchValueKey || (field.fetchStoreKey ?? "");
			return Object.values(field.fetchStore).filter((item) => {
				const inputText = this.fetchSelectState[field.key]?.inputText || "";
				return String(item[field.fetchStoreKey ?? ""]).toLowerCase().includes(inputText.toLowerCase());
			}).map((item) => [item[fetchValueKey], item[field.fetchStoreKey ?? ""]]);
		},
		getSortedOptions(field: FormLabel) {
			if (!field.options) {
				return [];
			}
			
			const entries = Object.entries(field.options).map(([key, value]: [string, any]) => {
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
			dropdownOpen: {} as Record<string, boolean>,
			dropdownPositions: {} as Record<string, { top: number; left: number; width: number }>,
			fetchSelectState: {} as Record<string, { inputText: string; isOpen: boolean; position: { top: number; left: number; width: number } | null }>,
			fetchSelectDebouncers: {} as Record<string, (...args: any[]) => void>,
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