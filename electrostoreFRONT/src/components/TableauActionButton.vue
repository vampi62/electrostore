<template>
	<template v-if="loading && button?.animation">
		<span :class="button.class" class="m-1">
			<span class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin inline-block">
			</span>
		</span>
	</template>
	<template v-else>
		<button @click="action" :class="button.class" class="m-1">
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

<script>
export default {
	name: "TableauActionButton",
	props: {
		button: {
			type: Object,
			required: true,
		},
		row: {
			type: Object,
			required: true,
		},
	},
	methods: {
		async action() {
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