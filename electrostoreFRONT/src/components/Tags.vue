<template>
	<div class="flex-1 min-h-96 bg-gray-200 px-2 py-2 rounded">
		<span v-for="(value, key) in sortedTags" :key="key"
			class="bg-gray-300 p-1 rounded mr-2 mb-1">
			{{ this.tagsStore[value][this.meta["keyName"]] }} ({{ this.tagsStore[value][this.meta["keyPoids"]] }})
			<span @click="deleteFunction(value)"
				class="text-red-500 cursor-pointer hover:text-red-600">
				<font-awesome-icon icon="fa-solid fa-times" />
			</span>
		</span>
		<span v-if="canEdit" class="bg-gray-300 p-1 rounded mr-2 mb-2">
			<span @click="tagOpenAddModal"
				class="text-green-500 cursor-pointer hover:text-green-600">
				<font-awesome-icon icon="fa-solid fa-plus" />
			</span>
		</span>
	</div>
	<teleport to="body">
		<div v-if="tagModalShow" class="fixed inset-0 bg-gray-800 bg-opacity-50 flex items-center justify-center z-50"
			@click="tagModalShow = false">
			<div class="flex flex-col bg-white rounded-lg shadow-lg w-3/4 h-3/4 overflow-y-hidden p-6" @click.stop>
				<div class="flex justify-between items-center border-b pb-3">
					<h2 class="text-2xl font-semibold">{{ $t('components.VModalTagAddTag') }}</h2>
					<button type="button" @click="tagModalShow = false"
						class="text-gray-500 hover:text-gray-700">&times;</button>
				</div>

				<!-- Filtres -->
				<FilterContainer class="my-4 flex gap-4" :filters="filterModal" :store-data="tagsStore" @output-filter="updateFilteredTags" />

				<!-- Tableau Items -->
				<Tableau :labels="tableauModal['label']" :meta="tableauModal['meta']"
					:store-data="[filteredTags, currentTags, ...otherStore]"
					:loading="tableauModal['loading']"
					:tableau-css="tableauModal['css']"
				/>
			</div>
		</div>
	</teleport>
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
		fetchFunction: {
			type: Function,
			default: () => {},
		},
		totalCount: {
			type: Number,
			default: 0,
		},
		canEdit: {
			type: Boolean,
			default: false,
		},
		tableauModal: {
			type: Object,
			required: true,
			default: null,
		},
		filterModal: {
			type: Object,
			required: true,
			default: null,
		},
		otherStore: {
			type: Array,
			required: false,
			default: () => [],
		},
		meta: {
			type: Object,
			required: true,
			default: null,
		},
	},
	computed:{
		sortedTags() {
			return Object.keys(this.currentTags || {})
				.sort((a, b) => this.tagsStore[b][this.meta["keyPoids"]] - this.tagsStore[a][this.meta["keyPoids"]]);
		},
	},
	data() {
		return {
			tagModalShow: false,
			tagLoad: false,
			filteredTags: [],
		};
	},
	methods: {
		async fetchAllTags() {
			let offset = 0;
			const limit = 100;
			do {
				await this.fetchFunction(offset, limit);
				offset += limit;
			} while (offset < this.totalCount);
			this.tagLoad = true;
		},
		tagOpenAddModal() {
			this.tagModalShow = true;
			if (!this.tagLoad) {
				this.fetchAllTags();
			}
		},
		updateFilteredTags(newValue) {
			this.filteredTags = newValue;
		},
	},
};
</script>