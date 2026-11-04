<template>
	<td v-for="(column,index) in labels"
		:key="index"
		:class="[css, column.type == 'text' ? 'text-left' : 'text-center']"
	>
		<TableauCell
			:column="column"
			:row="effectiveRow"
			:schema="schema"
			:store-data="storeData"
			:store-edition="storeEdition"
			:store-ready="storeReady"
		/>
	</td>
</template>

<script lang="ts">
import type { PropType } from "vue";
import type { StateTree } from "pinia";
import { mergeReadyRow } from "@/helpers";
import TableauCell from "@/components/TableauCell.vue";
import type { FormSchema } from "@/types/form";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
export default {
	name: "TableauRow",
	props: {
		labels: {
			type: Array as PropType<TableauLabel[]>,
			required: true,
			// labels for the columns, each object should have a key and type property
		},
		row: {
			type: Object as PropType<TableauRowData>,
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
	},
	components: {
		TableauCell,
	},
	computed: {
		effectiveRow() {
			return mergeReadyRow(this.row, this.storeReady);
		},
	},
};
</script>
