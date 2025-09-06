<template>
	<div class="bg-gray-100 p-2 rounded"
		:class="{ 'mb-6': !disableMargin }">
		<h3 @click="toggleSection" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
			:class="{ 'cursor-pointer': idPage != 'new', 'cursor-not-allowed': idPage == 'new' }">
			{{ $t(title) }} <span v-if="totalCount >= 0">({{ totalCount }})</span>
		</h3>
		<div :class="showSection ? 'block' : 'hidden'" class="p-2">
			<slot name="append-row"></slot>
		</div>
	</div>
</template>

<script>
export default {
	name: "CollapsibleSection",
	props: {
		idPage: {
			type: String,
			required: true,
			default: "new",
		},
		totalCount: {
			type: Number,
			required: false,
			default: -1,
		},
		title: {
			type: String,
			required: false,
			default: "",
		},
		disableMargin: {
			type: Boolean,
			required: false,
			default: false,
		},
	},
	data() {
		return {
			showSection: this.idPage !== "new",
		};
	},
	methods: {
		toggleSection() {
			if (this.idPage !== "new") {
				this.showSection = !this.showSection;
			}
		},
	},
};
</script>