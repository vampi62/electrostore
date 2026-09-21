<template>
	<div class="flex-1 min-h-96 bg-gray-200 px-2 py-2 rounded">
		<span v-for="key in sortedTags" :key="key"
			class="p-1 rounded mr-2 mb-1" :class="tagPillClass(key)">
			{{ this.tagsStore[key]?.[this.meta["keyName"]] }} ({{ this.tagsStore[key]?.[this.meta["keyPoids"]] }})
			<span v-if="tagStatus(key) === 'deleted'" @click="restoreFunction(key)"
				class="text-blue-500 cursor-pointer hover:text-blue-600">
				<font-awesome-icon icon="fa-solid fa-rotate-left" />
			</span>
			<span v-else @click="deleteFunction(key)"
				class="text-red-500 cursor-pointer hover:text-red-600">
				<font-awesome-icon icon="fa-solid fa-times" />
			</span>
		</span>
		<span v-if="canEdit" class="bg-gray-300 p-1 rounded mr-2 mb-2">
			<span @click="tagModalShow = true"
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
				<FilterContainer class="my-4 flex gap-4" :filters="filterModal" :store-data="tagsStore" />

				<!-- Tableau Items -->
				<Tableau :labels="tableauModal['label']" :meta="tableauModal['meta']"
					:store-data="[tagsStore, currentTags, ...otherStore]"
					:store-ready="effectiveTags"
					:filters="filterModal"
					:loading="tableauModal['loading']"
					:fetch-function="tableauModal['fetchFunction']"
					:total-count="tableauModal['totalCount']"
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
		readyStore: {
			type: Object,
			required: false,
			default: () => ({}),
			// readyStore is an object containing the tags pending addition/deletion, keyed by id_tag, e.g. { [id_tag]: { status: 'created' | 'deleted', ... } }
		},
		deleteFunction: {
			type: Function,
			default: () => {},
		},
		restoreFunction: {
			type: Function,
			default: () => {},
			// restoreFunction cancels a pending deletion staged in readyStore
		},
		fetchFunction: {
			type: Function,
			default: () => {},
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
		// merges currentTags (saved) with readyStore (pending, unsaved) without duplicate ids
		effectiveTags() {
			const merged = {};
			for (const key of Object.keys(this.currentTags || {})) {
				merged[key] = { status: this.readyStore?.[key]?.status || null };
			}
			for (const key of Object.keys(this.readyStore || {})) {
				if (!merged[key]) {
					merged[key] = { status: this.readyStore[key].status };
				}
			}
			return merged;
		},
		sortedTags() {
			return Object.keys(this.effectiveTags)
				.sort((a, b) => this.tagsStore[b]?.[this.meta["keyPoids"]] - this.tagsStore[a]?.[this.meta["keyPoids"]]);
		},
	},
	data() {
		return {
			tagModalShow: false,
			tagLoad: false,
		};
	},
	methods: {
		tagStatus(key) {
			return this.effectiveTags[key]?.status || null;
		},
		tagPillClass(key) {
			switch (this.tagStatus(key)) {
			case "created": return "bg-green-100 text-green-800";
			case "deleted": return "bg-red-100 text-red-800";
			case "modified": return "bg-amber-100 text-amber-800";
			default: return "bg-gray-300";
			}
		},
	},
};
</script>
