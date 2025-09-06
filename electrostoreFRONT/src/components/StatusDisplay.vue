<template>
	<div class="flex-1 w-96 min-h-96 bg-gray-200 px-4 py-2 rounded">
		<div v-for="(value, key) in dataStore" :key="key">
			<div v-if="key !== 'loading'">
				{{ key }}: {{ value }}
			</div>
		</div>
	</div>
</template>

<script>
export default {
	name: "StatusDisplay",
	props: {
		dataStore: {
			type: Object,   
			required: true,
			default: () => ({}),
			// 
		},
	},
};
</script>