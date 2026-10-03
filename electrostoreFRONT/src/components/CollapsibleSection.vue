<template>
	<div class="bg-gray-100 p-2 rounded"
		:class="{ 'mb-6': !disableMargin }">
		<h3 @click="toggleSection" class="text-xl font-semibold  bg-gray-400 p-2 rounded"
			:class="{ 'cursor-pointer': permission, 'cursor-not-allowed': !permission }">
			{{ $t(title) }} <span v-if="totalCount >= 0">({{ totalCount }})</span>
		</h3>
		<transition @before-enter="beforeEnter" @enter="enter" @after-enter="afterEnter" @leave="leave">
			<div v-show="showSection" class=" overflow-hidden">
				<div class="p-2">
					<slot name="append-row"></slot>
				</div>
			</div>
		</transition>
	</div>
</template>

<script lang="ts">
export default {
	name: "CollapsibleSection",
	props: {
		permission: {
			type: Boolean,
			required: false,
			default: true,
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
			showSection: this.permission,
		};
	},
	watch: {
		permission(newVal) {
			this.showSection = newVal;
		},
	},
	methods: {
		toggleSection() {
			if (this.permission) {
				this.showSection = !this.showSection;
			}
		},
		beforeEnter(el: Element) {
			(el as HTMLElement).style.height = "0";
		},
		afterEnter(el: Element) {
			(el as HTMLElement).style.height = "auto";
		},
		enter(el: Element) {
			const htmlEl = el as HTMLElement;
			htmlEl.style.height = "auto";
			const height = getComputedStyle(htmlEl).height;
			htmlEl.style.height = "0";
			requestAnimationFrame(() => {
				htmlEl.style.transition = "height 0.3s ease-in-out";
				htmlEl.style.height = height;
			});
		},
		leave(el: Element) {
			const htmlEl = el as HTMLElement;
			htmlEl.style.height = getComputedStyle(htmlEl).height;
			requestAnimationFrame(() => {
				htmlEl.style.transition = "height 0.3s ease-in-out";
				htmlEl.style.height = "0";
			});
		},
	},
};
</script>