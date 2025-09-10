<template>
	<div class="flex-1 min-h-96 bg-gray-200 px-2 py-2 rounded">
		<span v-for="(value, key) in sortedTags" :key="key"
			class="bg-gray-300 p-1 rounded mr-2 mb-1">
			{{ this.tagsStore[value].nom_tag }} ({{ this.tagsStore[value].poids_tag }})
			<span @click="deleteFunction(value)"
				class="text-red-500 cursor-pointer hover:text-red-600">
				<font-awesome-icon icon="fa-solid fa-times" />
			</span>
		</span>
		<span v-if="canEdit" class="bg-gray-300 p-1 rounded mr-2 mb-2">
			<span @click="$emit('openModalTag')"
				class="text-green-500 cursor-pointer hover:text-green-600">
				<font-awesome-icon icon="fa-solid fa-plus" />
			</span>
		</span>
	</div>
</template>

<script>
export default {
	name: "Tags",
	props: {
		tagsStore: {
			type: Object,
			required: true,
			default: null,
			// 
		},
		currentTags: {
			type: Object,
			required: true,
			default: null,
			// 
		},
		deleteFunction: {
			type: Function,
			default: () => {},
		},
		canEdit: {
			type: Boolean,
			default: false,
		},
	},
	computed:{
		sortedTags() {
			return Object.keys(this.currentTags || {})
				.sort((a, b) => this.tagsStore[b].poids_tag - this.tagsStore[a].poids_tag);
		},
	},
	emits: [
		"openModalTag",
	],
};
</script>