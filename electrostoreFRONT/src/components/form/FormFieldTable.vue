<template>
	<table class="min-w-full border border-gray-300">
		<thead>
			<tr>
				<th v-for="column in field.columns" :key="column.key" class="border border-gray-300 px-2 py-1">{{ $t(column.label) }}</th>
			</tr>
		</thead>
		<tbody>
			<tr v-for="(row, rowIndex) in rows" :key="rowIndex">
				<td v-for="column in field.columns" :key="column.key" class="border border-gray-300 px-2 py-1">
					<input
						v-if="column.type === 'text' || column.type === 'number'"
						:type="column.type"
						:value="row[column.key]"
						class="w-full border border-gray-300 rounded px-2 py-1"
						@input="onCellInput(rowIndex, column, $event)"
					/>
					<select
						v-else-if="column.type === 'select'"
						class="w-full border border-gray-300 rounded px-2 py-1"
						@change="onCellInput(rowIndex, column, $event)"
					>
						<template v-for="[index, option, optionDisabled, hidden] in getSortedOptions(column)" :key="index">
							<option :value="index" :selected="String(row[column.key]) === index" :disabled="optionDisabled" v-show="!hidden">{{ option }}</option>
						</template>
					</select>
					<textarea
						v-else-if="column.type === 'textarea'"
						:value="row[column.key]"
						class="w-full border border-gray-300 rounded px-2 py-1"
						@input="onCellInput(rowIndex, column, $event)"
					></textarea>
				</td>
				<td>
					<button type="button" class="bg-red-500 text-white px-4 py-2 rounded" @click="removeRow(rowIndex)">
						{{ $t('components.FormContainerDeleteRow') }}
					</button>
				</td>
			</tr>
			<tr>
				<td :colspan="field.columns?.length ?? 1" class="border border-gray-300 px-2 py-1">
					<button type="button" class="bg-regal-bleu-ciel-100 text-white px-4 py-2 rounded" @click="addRow">
						{{ $t('components.FormContainerAddRow') }}
					</button>
				</td>
			</tr>
		</tbody>
	</table>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { getSortedOptions } from "@/utils/formOptions";
import type { FormLabel, FormTableauColumn } from "@/types/form";

type TableRow = Record<string, string | number | undefined>;

// `disabled` and `error` are part of the common contract of the fields, unused here
const props = defineProps<{ field: FormLabel; inputId: string; disabled?: boolean; error?: string; modelValue?: unknown }>();
const emit = defineEmits<{ "update:modelValue": [value: TableRow[]] }>();

const rows = computed<TableRow[]>(() => (Array.isArray(props.modelValue) ? props.modelValue : []));

function onCellInput(rowIndex: number, column: FormTableauColumn, event: Event) {
	const target = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
	let value: string | number = target.value;
	if (column.type === "number") {
		const parsed = Number.parseFloat(value);
		value = Number.isNaN(parsed) ? value : parsed;
	}
	emit("update:modelValue", rows.value.map((row, index) => (index === rowIndex ? { ...row, [column.key]: value } : row)));
}

function addRow() {
	emit("update:modelValue", [...rows.value, {}]);
}

function removeRow(rowIndex: number) {
	emit("update:modelValue", rows.value.filter((_, index) => index !== rowIndex));
}
</script>
