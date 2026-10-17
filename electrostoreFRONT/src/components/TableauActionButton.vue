<template>
	<template v-if="loading && button?.animation">
		<span :class="button.class" class="m-1">
			<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin inline-block">
			</span>
		</span>
	</template>
	<template v-else>
		<button 
			@click="action" 
			:class="[button.class, { 'disabled-button': disabled }]" 
			:disabled="disabled"
			class="m-1">
			<span v-if="button.icon">
				<font-awesome-icon :icon="button.icon" />
				<span v-if="button.label" class="mr-2"></span>
			</span>
			<span v-if="button.label">
				{{ $t(button.label) }}
			</span>
		</button>
	</template>
</template>

<script lang="ts">
import type { PropType } from "vue";
import type { TableauButton, TableauRowData } from "@/types/tableau";
export default {
	name: "TableauActionButton",
	props: {
		button: {
			type: Object as PropType<TableauButton>,
			required: true,
		},
		row: {
			type: Object as PropType<TableauRowData>,
			required: true,
		},
		disabled: {
			type: Boolean,
			default: false,
		},
	},
	methods: {
		async action() {
			if (this.disabled) {
				return;
			}
			this.loading = true;
			await this.button.action(this.row);
			this.loading = false;
		},
	},
	data() {
		return {
			loading: false,
		};
	},
};
</script>

<style scoped>
.disabled-button {
	background-color: #9ca3af;
	cursor: not-allowed;
	opacity: 0.6;
}

.disabled-button:hover {
	background-color: #9ca3af;
}
</style>