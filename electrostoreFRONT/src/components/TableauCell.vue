<template>
	<template v-if="column.type == 'bool'">
		<template v-if="evaluateCondition(column.condition ?? 'false', row)">
			<font-awesome-icon icon="fa-solid fa-check" class="text-green-500" />
		</template>
		<template v-else>
			<font-awesome-icon icon="fa-solid fa-times" class="text-red-500" />
		</template>
	</template>
	<template v-else-if="column.type == 'link-list'">
		<ul>
			<li v-for="(item, itemIndex) in getDataLinkListValue(row,column) || []"
				:key="itemIndex">
				{{ item }}
			</li>
		</ul>
	</template>
	<template v-else-if="column.type == 'link-data'">
		{{ getDataLinkValue(row,column) }}
	</template>
	<template v-else-if="column.type == 'image'">
		<div class="flex justify-center items-center">
			<template v-if="column.storeLinkId && getImageLinkId(row, column) !== null">
				<img v-if="getImageSrc(row, column)"
					:src="getImageSrc(row, column)"
					:class="imageClass" :alt="`Id ${row[column.key]}`" />
				<span v-else :class="imageClass">
					<div class="loading-spinner">
						<div class="w-16 h-16 spinner-ring"></div>
					</div>
				</span>
			</template>
			<template v-else-if="!column.storeLinkId && column.sourceKey && row?.[column.sourceKey] && column.fieldUrl && getImageSrc(row, column)">
				<img :src="getImageSrc(row, column)"
					:class="imageClass" :alt="`Id ${row[column.key]}`" />
			</template>
			<template v-else>
				<img src="../assets/nopicture.webp" alt="Unavailable" :class="imageClass" />
			</template>
		</div>
	</template>
	<template v-else-if="column.type == 'buttons'">
		<div class="flex justify-center items-center">
			<template v-for="(button, buttonIndex) in column.buttons" :key="buttonIndex">
				<template v-if="!button?.showCondition || evaluateCondition(button.showCondition, row)">
					<TableauActionButton
						:button="button"
						:row="row"
						:disabled="Boolean(button?.enableCondition) && !evaluateCondition(String(button?.enableCondition ?? ''), row)"
					/>
				</template>
			</template>
		</div>
	</template>
	<template v-else-if="!readonly && column.canEdit && column.valueKey && storeEdition?.[column.valueKey] !== undefined">
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
		<span v-html="formatCellValue(column, getDataValue(row, column))"></span>
	</template>
</template>

<script lang="ts">
import { defineAsyncComponent } from "vue";
import type { PropType } from "vue";
import type { StateTree } from "pinia";
import { Form, Field } from "vee-validate";
import type { FormSchema } from "@/types/form";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
export default {
	name: "TableauCell",
	props: {
		column: {
			type: Object as PropType<TableauLabel>,
			required: true,
			// label of the column to print
		},
		row: {
			type: Object as PropType<TableauRowData>,
			required: true,
			// row to print, already merged with the unsaved changes (storeReady)
		},
		schema: {
			type: Object as PropType<FormSchema>,
			required: false,
			// Validation schema for vee-validate
			default: undefined,
		},
		storeData: {
			type: Array as PropType<StateTree[]>,
			required: false,
			// storeData pass by the parent component, containing the data from the stores, used for link-list and image types
			default: () => [],
		},
		storeEdition: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
			// storeEdition is the edition of the row (the fields being edited), pass by the parent component
		},
		storeReady: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
			// storeReady is the unsaved change of the row (status and fields), pass by the parent component
		},
		readonly: {
			type: Boolean,
			default: false,
			// when true, canEdit is ignored and the value is only printed (card view)
		},
		imageClass: {
			type: String,
			default: "w-16 h-16 object-cover rounded",
			// tailwind classes of the image
		},
	},
	components: {
		Form,
		Field,
		TableauActionButton: defineAsyncComponent(() => import("@/components/TableauActionButton.vue")),
	},
	methods: {
		evaluateCondition(condition: string, rowData: TableauRowData) {
			try {
				return new Function("store", "edition", "ready", "rowData", `return ${condition}`)(this.storeData, this.storeEdition, this.storeReady, rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		formatCellValue(column: TableauLabel, data: unknown) {
			switch (column.type) {
			case "text":
				return data;
			case "enum":
				return column.options?.[String(data)];
			case "date":
				return data ? new Date(data as string | number).toLocaleDateString() : "";
			case "datetime":
				return data ? new Date(data as string | number).toLocaleString() : "";
			default:
				return data;
			}
		},
		getImageLinkId(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, sourceKey, storeLinkKeyJoinRessource } = label;
			if (!storeLinkId || !sourceKey || !storeLinkKeyJoinRessource) {
				return undefined;
			}
			return this.storeData[storeLinkId]?.[row[sourceKey]]?.[storeLinkKeyJoinRessource];
		},
		getImageSrc(row: TableauRowData, label: TableauLabel) {
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
		printRessource(label: TableauLabel, linkedItem: TableauRowData | undefined) {
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
		getDataLinkListValue(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, sourceKey } = label;
			if (!storeLinkId || !sourceKey) {
				return [];
			}
			return Object.values<TableauRowData>(this.storeData[storeLinkId]?.[row[sourceKey]] || {}).map((linkedItem) => this.printRessource(label, linkedItem));
		},
		getDataLinkValue(row: TableauRowData, label: TableauLabel) {
			const { storeLinkId, sourceKey } = label;
			if (!storeLinkId || !sourceKey) {
				return "";
			}
			return this.printRessource(label, this.storeData[storeLinkId]?.[row[sourceKey]]);
		},
		getDataValue(row: TableauRowData, label: TableauLabel) {
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
