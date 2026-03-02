<template>
	<td v-for="(column,index) in labels"
		:key="index"
		:class="[css, column.type == 'text' ? 'text-left' : 'text-center']"
	>
		<template v-if="column.type == 'bool'">
			<template v-if="row[column.key]">
				<font-awesome-icon icon="fa-solid fa-check" class="text-green-500" />
			</template>
			<template v-else>
				<font-awesome-icon icon="fa-solid fa-times" class="text-red-500" />
			</template>
		</template>
		<template v-else-if="column.type == 'list' || column.type == 'link-list'">
			<ul>
				<li v-for="(item, itemIndex) in row[column.key] || []"
					:key="itemIndex">
					{{ item }}
				</li>
			</ul>
		</template>
		<template v-else-if="column.type == 'image'">
			<div class="flex justify-center items-center">
				<template v-if="this.row?.[column.key]">
					<img v-if="this.row?.[column.imgKey]"
						:src="this.row?.[column.imgKey]"
						class="w-16 h-16 object-cover rounded" :alt="`Id ${row[column.key]}`" />
					<span v-else class="w-16 h-16 object-cover rounded">
						{{ $t('components.VModalTableauImageLoading') }}
					</span>
				</template>
				<template v-else>
					<img src="../assets/nopicture.webp" alt="Unavailable" class="w-16 h-16 object-cover rounded" />
				</template>
			</div>
		</template>
		<template v-else-if="column.type == 'buttons'">
			<div class="flex justify-center items-center">
				<template v-for="(button, buttonIndex) in row[column.btKey]" :key="buttonIndex">
					<template v-if="button?.show">
						<TableauActionButton :button="button" :row="row" />
					</template>
				</template>
			</div>
		</template>
		<template v-else-if="column.canEdit && row?.tmp">
			<Form :validation-schema="schema" v-slot="{ errors }">
				<Field
					:name="column.key"
					v-model="row.tmp[column.key]"
					:type="column.type"
					:class="['w-20 p-2 border rounded-lg', errors[column.key] ? 'border-red-500' : '']"
					:placeholder="column.placeholder || ''"
					:options="column.options || []"
				/>
			</Form>
		</template>
		<template v-else>
			<span v-html="formatCellValue(column, row[column.key])"></span>
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
	},
	components: {
		Form,
		Field,
		TableauActionButton: defineAsyncComponent(() => import("@/components/TableauActionButton.vue")),
	},
	methods: {
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
	},
};
</script>