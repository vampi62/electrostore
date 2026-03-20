<template>
	<td v-for="(column,index) in labels"
		:key="index"
		:class="[css, column.type == 'text' ? 'text-left' : 'text-center']"
	>
		<template v-if="column.type == 'bool'">
			<template v-if="evaluateCondition(column.condition, row)">
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
				<template v-if="column.storeLinkId && storeData[column.storeLinkId]?.[row[column.sourceKey]]?.[column.storeLinkKeyJoinRessource] !== null">
					<img v-if="storeData[column.storeRessourceId]?.[storeData[column.storeLinkId]?.[row[column.sourceKey]]?.[column.storeLinkKeyJoinRessource]]"
						:src="storeData[column.storeRessourceId]?.[storeData[column.storeLinkId]?.[row[column.sourceKey]]?.[column.storeLinkKeyJoinRessource]]"
						class="w-16 h-16 object-cover rounded" :alt="`Id ${row[column.key]}`" />
					<span v-else class="w-16 h-16 object-cover rounded">
						<div class="loading-spinner">
							<div class="w-16 h-16 spinner-ring"></div>
						</div>
					</span>
				</template>
				<template v-else-if="!column.storeLinkId && row?.[column.sourceKey]">
					<img v-if="storeData[column.storeRessourceId]?.[row[column.sourceKey]]"
						:src="storeData[column.storeRessourceId]?.[row[column.sourceKey]]"
						class="w-16 h-16 object-cover rounded" :alt="`Id ${row[column.key]}`" />
					<span v-else class="w-16 h-16 object-cover rounded">
						<div class="loading-spinner">
							<div class="w-16 h-16 spinner-ring"></div>
						</div>
					</span>
				</template>
				<template v-else>
					<img src="../assets/nopicture.webp" alt="Unavailable" class="w-16 h-16 object-cover rounded" />
				</template>
			</div>
		</template>
		<template v-else-if="column.type == 'buttons'">
			<div class="flex justify-center items-center">
				<template v-for="(button, buttonIndex) in column.buttons" :key="buttonIndex">
					<template v-if="!button?.showCondition || evaluateCondition(button.showCondition, row)">
						<TableauActionButton :button="button" :row="row" :disabled="button?.enableCondition && !evaluateCondition(button.enableCondition)" />
					</template>
				</template>
			</div>
		</template>
		<template v-else-if="column.canEdit && row?.tmp">
			<Form :validation-schema="schema" v-slot="{ errors }">
				<Field
					:name="column.valueKey"
					v-model="row.tmp[column.valueKey]"
					:type="column.type"
					:class="['w-20 p-2 border rounded-lg', errors[column.valueKey] ? 'border-red-500' : '']"
					:placeholder="column.placeholder || ''"
					:options="column.options || []"
				/>
			</Form>
		</template>
		<template v-else>
			<span v-html="formatCellValue(column, getDataValue(row, column))"></span>
		</template>
	</td>
</template>

<script>
import { defineAsyncComponent } from "vue";
import { Form, Field } from "vee-validate";
export default {
	name: "TableauRow",
	props: {
		labels: {
			type: Array,
			required: true,
			// labels for the columns, each object should have a key and type property
			default: () => [],
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
	},
	components: {
		Form,
		Field,
		TableauActionButton: defineAsyncComponent(() => import("@/components/TableauActionButton.vue")),
	},
	methods: {
		evaluateCondition(condition,rowData) {
			try {
				return new Function(["store","rowData"], `return ${condition}`)(this.storeData,rowData);
			} catch (error) {
				console.error("Erreur lors de l'évaluation de la condition :", error);
				return false;
			}
		},
		formatCellValue(column, data) {
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
		getDataLinkListValue(row, label) {
			return Object.values(this.storeData[label.storeLinkId]?.[row[label.sourceKey]] || {}).map((linkedItem) => {
				let printedRessource = "";
				label.ressourcePrint.forEach((print) => {
					if (print.from === "ressource") {
						printedRessource += this.storeData[label.storeRessourceId]?.[linkedItem[label.storeLinkKeyJoinRessource]]?.[print.valueKey] || "";
					} else if (print.from === "link") {
						printedRessource += linkedItem?.[print.valueKey] || "";
					} else if (print.from === "text") {
						printedRessource += print.text || "";
					}
				});
				return printedRessource;
			});
		},
		getDataLinkValue(row, label) {
			const linkedItem = this.storeData[label.storeLinkId]?.[row[label.sourceKey]];
			let printedRessource = "";
			label.ressourcePrint.forEach((print) => {
				if (print.from === "ressource") {
					printedRessource += this.storeData[label.storeRessourceId]?.[linkedItem[label.storeLinkKeyJoinRessource]]?.[print.valueKey] || "";
				} else if (print.from === "link") {
					printedRessource += linkedItem?.[print.valueKey] || "";
				} else if (print.from === "text") {
					printedRessource += print.text || "";
				}
			});
			return printedRessource;
		},
		getDataValue(row, label) {
			if (label.storeRessourceId && label.storeLinkId) {
				const linkedItem = this.storeData[label.storeLinkId]?.[row[label.sourceKey]];
				return this.storeData[label.storeRessourceId]?.[linkedItem?.[label.storeLinkKeyJoinRessource]]?.[label.valueKey];
			} else if (label.storeRessourceId && !label.storeLinkId) {
				return this.storeData[label.storeRessourceId]?.[row[label.sourceKey]]?.[label.valueKey];
			} else {
				return row?.[label.valueKey];
			}
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