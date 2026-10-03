<template>
	<td v-for="(column,index) in labels"
		:key="index"
		:class="[css, column.type == 'text' ? 'text-left' : 'text-center']"
	>
		<template v-if="column.type == 'bool'">
			<template v-if="evaluateCondition(column.condition ?? 'false', effectiveRow)">
				<font-awesome-icon icon="fa-solid fa-check" class="text-green-500" />
			</template>
			<template v-else>
				<font-awesome-icon icon="fa-solid fa-times" class="text-red-500" />
			</template>
		</template>
		<template v-else-if="column.type == 'link-list'">
			<ul>
				<li v-for="(item, itemIndex) in getDataLinkListValue(effectiveRow,column) || []"
					:key="itemIndex">
					{{ item }}
				</li>
			</ul>
		</template>
		<template v-else-if="column.type == 'link-data'">
			{{ getDataLinkValue(effectiveRow,column) }}
		</template>
		<template v-else-if="column.type == 'image'">
			<div class="flex justify-center items-center">
				<template v-if="column.storeLinkId && getImageLinkId(effectiveRow, column) !== null">
					<img v-if="getImageSrc(effectiveRow, column)"
						:src="getImageSrc(effectiveRow, column)"
						class="w-16 h-16 object-cover rounded" :alt="`Id ${effectiveRow[column.key]}`" />
					<span v-else class="w-16 h-16 object-cover rounded">
						<div class="loading-spinner">
							<div class="w-16 h-16 spinner-ring"></div>
						</div>
					</span>
				</template>
				<template v-else-if="!column.storeLinkId && column.sourceKey && effectiveRow?.[column.sourceKey] && column.fieldUrl && getImageSrc(effectiveRow, column)">
					<img :src="getImageSrc(effectiveRow, column)"
						class="w-16 h-16 object-cover rounded" :alt="`Id ${effectiveRow[column.key]}`" />
				</template>
				<template v-else>
					<img src="../assets/nopicture.webp" alt="Unavailable" class="w-16 h-16 object-cover rounded" />
				</template>
			</div>
		</template>
		<template v-else-if="column.type == 'buttons'">
			<div class="flex justify-center items-center">
				<template v-for="(button, buttonIndex) in column.buttons" :key="buttonIndex">
					<template v-if="!button?.showCondition || evaluateCondition(button.showCondition, effectiveRow)">
						<TableauActionButton
							:button="button"
							:row="effectiveRow"
							:disabled="Boolean(button?.enableCondition) && !evaluateCondition(String(button?.enableCondition ?? ''), effectiveRow)"
						/>
					</template>
				</template>
			</div>
		</template>
		<template v-else-if="column.canEdit && column.valueKey && storeEdition?.[column.valueKey] !== undefined">
			<Form :validation-schema="schema" v-slot="{ errors }">
				<Field
					:name="column.valueKey"
					v-model="storeEdition[column.valueKey]"
					:type="column.type"
					:class="['w-20 p-2 border rounded-lg', errors[column.valueKey] ? 'border-red-500' : '']"
					:placeholder="column.placeholder || ''"
					:options="column.options || []"
				/><br>
				<span class="text-red-500 h-5 w-full text-sm">{{ errors[column.valueKey] || ' ' }}</span>
			</Form>
		</template>
		<template v-else>
			<span v-html="formatCellValue(column, getDataValue(effectiveRow, column))"></span>
		</template>
	</td>
</template>

<script lang="ts">
import { defineAsyncComponent } from "vue";
import type { PropType } from "vue";
import { Form, Field } from "vee-validate";
import type { TableauLabel } from "@/types/tableau";
export default {
	name: "TableauRow",
	props: {
		labels: {
			type: Array as PropType<TableauLabel[]>,
			required: true,
			// labels for the columns, each object should have a key and type property
		},
		row: {
			type: Object,
			required: true,
			// row pass by the parent component, containing the data for the current row
		},
		css: {
			type: String,
			required: false,
			// Default CSS class tailwind for table cells
			default: "border border-gray-300 px-4 py-2 text-sm text-gray-700",
		},
		schema: {
			type: Object,
			required: false,
			// Validation schema for vee-validate
			default: () => ({}),
		},
		storeData: {
			type: Object,
			required: false,
			// storeData pass by the parent component, containing the data from the stores, used for link-list and image types
			default: () => ({}),
		},
		storeEdition: {
			type: Object,
			required: false,
			default: () => ({}),
			// storeEdition is an object containing the store and key to edit a resource when clicking on a row, it should have the properties storeEditionKey and storeEditionStore
		},
		storeReady: {
			type: Object,
			required: false,
			default: () => ({}),
			// storeReady is an object containing the store and key containing unsaved changes to prevent leaving the page, it should have the properties storeReadyKey and storeReadyStore
		},
	},
	components: {
		Form,
		Field,
		TableauActionButton: defineAsyncComponent(() => import("@/components/TableauActionButton.vue")),
	},
	computed: {
		effectiveRow() {
			if (!this.storeReady?.status || !this.storeReady?.data) {
				return this.row;
			}
			return {
				...this.row,
				...Object.fromEntries(
					Object.entries(this.storeReady.data).filter(([, v]) => v !== undefined && v !== null),
				),
			};
		},
	},
	methods: {
		evaluateCondition(condition: string, rowData: Record<string, any>) {
			try {
				return new Function("store", "edition", "ready", "rowData", `return ${condition}`)(this.storeData, this.storeEdition, this.storeReady, rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		formatCellValue(column: any, data: any) {
			switch (column.type) {
			case "text":
				return data;
			case "enum":
				return column.options[data];
			case "date":
				return data ? new Date(data).toLocaleDateString() : "";
			case "datetime":
				return data ? new Date(data).toLocaleString() : "";
			default:
				return data;
			}
		},
		getImageLinkId(row: Record<string, any>, label: TableauLabel) {
			const { storeLinkId, sourceKey, storeLinkKeyJoinRessource } = label;
			if (!storeLinkId || !sourceKey || !storeLinkKeyJoinRessource) {
				return undefined;
			}
			return this.storeData[storeLinkId]?.[row[sourceKey]]?.[storeLinkKeyJoinRessource];
		},
		getImageSrc(row: Record<string, any>, label: TableauLabel) {
			const { storeRessourceId, storeLinkId, sourceKey } = label;
			if (!storeRessourceId || !sourceKey) {
				return undefined;
			}
			const key = storeLinkId ? this.getImageLinkId(row, label) : row[sourceKey];
			if (key === undefined || key === null) {
				return undefined;
			}
			return this.storeData[storeRessourceId]?.[key];
		},
		printRessource(label: TableauLabel, linkedItem: Record<string, any> | undefined) {
			const { storeRessourceId, storeLinkKeyJoinRessource } = label;
			let printedRessource = "";
			for (const print of label.ressourcePrint ?? []) {
				if (print.from === "ressource" && storeRessourceId && storeLinkKeyJoinRessource && print.valueKey) {
					printedRessource += this.storeData[storeRessourceId]?.[linkedItem?.[storeLinkKeyJoinRessource]]?.[print.valueKey] || "";
				} else if (print.from === "link" && print.valueKey) {
					printedRessource += linkedItem?.[print.valueKey] || "";
				} else if (print.from === "text") {
					printedRessource += print.text || "";
				}
			}
			return printedRessource;
		},
		getDataLinkListValue(row: Record<string, any>, label: TableauLabel) {
			const { storeLinkId, sourceKey } = label;
			if (!storeLinkId || !sourceKey) {
				return [];
			}
			return (Object.values(this.storeData[storeLinkId]?.[row[sourceKey]] || {}) as Record<string, any>[]).map((linkedItem) => this.printRessource(label, linkedItem));
		},
		getDataLinkValue(row: Record<string, any>, label: TableauLabel) {
			const { storeLinkId, sourceKey } = label;
			if (!storeLinkId || !sourceKey) {
				return "";
			}
			return this.printRessource(label, this.storeData[storeLinkId]?.[row[sourceKey]]);
		},
		getDataValue(row: Record<string, any>, label: TableauLabel) {
			const { storeRessourceId, storeLinkId, sourceKey, valueKey, storeLinkKeyJoinRessource } = label;
			if (!valueKey) {
				return undefined;
			}
			if (storeRessourceId && storeLinkId) {
				if (!sourceKey || !storeLinkKeyJoinRessource) {
					return undefined;
				}
				const linkedItem = this.storeData[storeLinkId]?.[row[sourceKey]];
				return this.storeData[storeRessourceId]?.[linkedItem?.[storeLinkKeyJoinRessource]]?.[valueKey];
			} else if (storeRessourceId) {
				if (!sourceKey) {
					return undefined;
				}
				return this.storeData[storeRessourceId]?.[row[sourceKey]]?.[valueKey];
			}
			return row?.[valueKey];
		},
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