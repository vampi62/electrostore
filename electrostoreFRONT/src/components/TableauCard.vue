<template>
	<div class="flex flex-col rounded-lg border border-gray-200 shadow-sm overflow-hidden transition duration-150 hover:shadow-md"
		:class="[statusClass || 'bg-white', clickable ? 'cursor-pointer' : '']">
		<div v-if="roles.image" class="bg-gray-100 p-2">
			<TableauCell v-bind="cellProps(roles.image)" image-class="w-full h-40 object-contain rounded" />
		</div>
		<div class="flex flex-col gap-1 p-3 flex-1">
			<div v-if="roles.title" class="font-semibold text-base break-words">
				<TableauCell v-bind="cellProps(roles.title)" />
			</div>
			<div v-if="roles.subtitle" class="text-sm text-gray-500 break-words">
				<TableauCell v-bind="cellProps(roles.subtitle)" />
			</div>
			<dl v-if="roles.fields.length" class="mt-1 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-sm">
				<template v-for="(column, index) in roles.fields" :key="index">
					<dt class="text-gray-500">{{ $t(column.label) }}</dt>
					<dd class="break-words"><TableauCell v-bind="cellProps(column)" /></dd>
				</template>
			</dl>
		</div>
		<div v-if="roles.buttons.length" class="border-t border-gray-200 p-1 flex flex-wrap justify-end" @click.stop>
			<TableauCell v-for="(column, index) in roles.buttons" :key="index" v-bind="cellProps(column)" />
		</div>
	</div>
</template>

<script lang="ts">
import type { PropType } from "vue";
import type { StateTree } from "pinia";
import { mergeReadyRow } from "@/helpers";
import TableauCell from "@/components/TableauCell.vue";
import type { TableauLabel, TableauRowData } from "@/types/tableau";
export default {
	name: "TableauCard",
	props: {
		labels: {
			type: Array as PropType<TableauLabel[]>,
			required: true,
			// labels of the columns, cardRole tells where each one is printed in the card
		},
		row: {
			type: Object as PropType<TableauRowData>,
			required: true,
		},
		storeData: {
			type: Array as PropType<StateTree[]>,
			required: false,
			default: () => [],
		},
		storeEdition: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
		},
		storeReady: {
			type: Object as PropType<StateTree>,
			required: false,
			default: () => ({}),
		},
		statusClass: {
			type: String,
			default: "",
			// tailwind classes (background/text) of the row status (created, modified, deleted)
		},
		clickable: {
			type: Boolean,
			default: false,
		},
	},
	components: {
		TableauCell,
	},
	computed: {
		effectiveRow() {
			return mergeReadyRow(this.row, this.storeReady);
		},
		roles() {
			const visible = this.labels.filter((label) => label.cardRole !== "hidden");
			// without an explicit cardRole, the first image column is the image and the first text column the title
			const image = visible.find((l) => l.cardRole === "image") ?? visible.find((l) => !l.cardRole && l.type === "image");
			const title = visible.find((l) => l.cardRole === "title") ?? visible.find((l) => !l.cardRole && l.type === "text" && l !== image);
			const subtitle = visible.find((l) => l.cardRole === "subtitle");
			const buttons = visible.filter((l) => l.type === "buttons");
			const fields = visible.filter((l) => l !== image && l !== title && l !== subtitle && l.type !== "buttons");
			return { image, title, subtitle, buttons, fields };
		},
	},
	methods: {
		cellProps(column: TableauLabel) {
			return {
				column,
				row: this.effectiveRow,
				storeData: this.storeData,
				storeEdition: this.storeEdition,
				storeReady: this.storeReady,
				readonly: true,
			};
		},
	},
};
</script>
