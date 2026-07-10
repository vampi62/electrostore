<template>
	<div class="p-4" :class="[isHorizontal ? 'w-full' : 'w-60']">
		<div class="hidden lg:block">
			<div class="rounded-lg overflow-hidden shadow-lg border-2" :class="[isHorizontal ? 'flex' : 'flex flex-col', getCurrentStepBorderClass()]">
				<div
					v-for="(step, index) in steps"
					:key="step.id"
					class="relative cursor-pointer transition-all duration-300 overflow-visible z-10"
					:class="[isHorizontal ? 'flex-1' : 'w-full', getSegmentClass(index)]"
					@mouseenter="showHistory(step, index)"
					@mouseleave="hideHistory($event, index)"
					:ref="`step-${index}`"
				>
					<div class="h-24 flex flex-col items-center justify-center p-4 relative">
						<div 
							class="text-sm font-semibold text-center leading-tight"
							:class="getSegmentTextClass(index)"
						>
							{{ step.name }}
						</div>
						<div 
							v-if="index < steps.length - 1"
							class="absolute bg-white opacity-30"
							:class="isHorizontal ? 'right-0 top-2 bottom-2 w-px' : 'bottom-0 left-2 right-2 h-px'"
						></div>
					</div>
					<teleport to="body">
						<div
							v-if="getStepHistory(step) && hoveredStep === index"
							:ref="`history-${index}`"
							class="absolute bg-white shadow-2xl rounded-lg p-4 w-80 z-50 border-2"
							:class="getCurrentStepBorderClass()"
							@mouseleave="hideHistory($event, index)"
						>
							<div class="flex items-center justify-between mb-3">
								<h4 class="font-bold text-sm text-gray-800">
									Historique - {{ step.name }}
								</h4>
								<span
									class="text-xs px-3 py-1 rounded-full font-semibold"
									:class="getCurrentStepBadgeClass()"
								>
									{{ steps[currentStep].name }}
								</span>
							</div>
							<div class="space-y-3 max-h-64 overflow-y-auto pr-2">
								<div
									v-for="(entry, idx) in getStepHistory(step)"
									:key="idx"
									class="border-l-4 pl-3 py-2 rounded-r bg-gray-50"
									:class="getCurrentStepHistoryBorderClass()"
								>
									<p class="font-medium text-gray-800 text-sm">{{ entry.action }}</p>
									<p class="text-gray-500 text-xs mt-1">
										<svg class="inline w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
										</svg>
										{{ formatDate(entry.date) }}
									</p>
									<p v-if="entry.user" class="text-gray-600 text-xs mt-1">
										<svg class="inline w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
										</svg>
										{{ entry.user }}
									</p>
									<p v-if="entry.comment" class="text-gray-700 text-xs mt-2 bg-white p-2 rounded border border-gray-200">
										{{ entry.comment }}
									</p>
								</div>
							</div>
						</div>
					</teleport>
				</div>
			</div>
			<div class="mt-4 flex items-center justify-center gap-2">
				<span class="text-sm font-semibold" :class="getCurrentStepTextClass()">
					Étape actuelle : {{ steps[currentStep].name }}
				</span>
			</div>
		</div>
		<div class="lg:hidden">
			<div class="rounded-lg overflow-hidden shadow-lg border-2" :class="getCurrentStepBorderClass()">
				<div
					v-for="(step, index) in steps"
					:key="step.id"
					class="relative cursor-pointer transition-all duration-300"
					:class="getSegmentClass(index)"
					@click="toggleHistory(index)"
				>
					<div class="p-4 flex items-center justify-between">
						<div class="flex items-center gap-4 flex-1">
							<div class="flex-1">
								<div 
									class="font-semibold"
									:class="getSegmentTextClass(index)"
								>
									{{ step.name }}
								</div>
							</div>
						</div>
						<div class="flex items-center gap-2">
							<svg
								v-if="getStepHistory(step)"
								class="w-5 h-5 transition-transform duration-300"
								:class="[
									expandedStep === index ? 'rotate-180' : '',
									getSegmentTextClass(index)
								]"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
							</svg>
						</div>
					</div>
					<div
						v-if="expandedStep === index && getStepHistory(step)"
						class="px-4 pb-4" @click.stop
					>
						<div class="border-t pt-3" :class="getCurrentStepHistoryBorderClass()">
							<h4 class="font-bold text-sm mb-3 text-gray-800">Historique</h4>
							<div class="space-y-3">
								<div
									v-for="(entry, idx) in getStepHistory(step)"
									:key="idx"
									class="border-l-4 pl-3 py-2 rounded-r bg-white bg-opacity-50"
									:class="getCurrentStepHistoryBorderClass()"
								>
									<p class="font-medium text-gray-800 text-sm">{{ entry.action }}</p>
									<p class="text-gray-500 text-xs mt-1">{{ formatDate(entry.date) }}</p>
									<p v-if="entry.user" class="text-gray-600 text-xs mt-1">
										Par: {{ entry.user }}
									</p>
									<p v-if="entry.comment" class="text-gray-700 text-xs mt-2 bg-white p-2 rounded">
										{{ entry.comment }}
									</p>
								</div>
							</div>
						</div>
					</div>
					<div 
						v-if="index < steps.length - 1"
						class="h-px bg-white opacity-30 mx-4"
					></div>
				</div>
			</div>
			<div class="mt-4 flex items-center justify-center gap-2">
				<span class="text-sm font-semibold" :class="getCurrentStepTextClass()">
					Étape : {{ steps[currentStep].name }}
				</span>
			</div>
		</div>
	</div>
</template>

<script>
export default {
	name: "RoadMap",
	props: {
		steps: {
			type: Array,
			required: true,
			// Format: [{ id: 1, name: 'NotStarted' }, { id: 2, name: 'InProgress' }, ...]
		},
		currentStep: {
			type: Number,
			required: true,
			// Index de l'étape actuelle (0-based)
		},
		mode: {
			type: String,
			default: "horizontal-bottom",
			validator: (value) => ["horizontal-top", "horizontal-bottom", "horizontal-left", "horizontal-right", "vertical-top", "vertical-bottom", "vertical-left", "vertical-right"].includes(value),
		},
		history: {
			type: Object,
			default: () => ({}),
			// Format: { stepId: [{ action: '', date: '', user: '', comment: '' }] }
		},
		stepColors: {
			type: Object,
			default: () => ({
				NotStarted: {
					completed: "bg-gray-300 text-gray-700",
					current: "bg-gray-400 text-gray-800",
					pending: "bg-gray-100 text-gray-500",
					border: "border-gray-400",
					badge: "bg-gray-200 text-gray-800",
					text: "text-gray-700",
					historyBorder: "border-gray-400",
				},
				OnHold: {
					completed: "bg-yellow-400 text-yellow-900",
					current: "bg-yellow-500 text-yellow-900",
					pending: "bg-yellow-100 text-yellow-700",
					border: "border-yellow-500",
					badge: "bg-yellow-200 text-yellow-900",
					text: "text-yellow-700",
					historyBorder: "border-yellow-500",
				},
				InProgress: {
					completed: "bg-blue-400 text-white",
					current: "bg-blue-500 text-white",
					pending: "bg-blue-100 text-blue-600",

					border: "border-blue-500",
					badge: "bg-blue-200 text-blue-900",
					text: "text-blue-700",
					historyBorder: "border-blue-500",
				},
				Completed: {
					completed: "bg-green-400 text-white",
					current: "bg-green-500 text-white",
					pending: "bg-green-100 text-green-600",
					border: "border-green-500",
					badge: "bg-green-200 text-green-900",
					text: "text-green-700",
					historyBorder: "border-green-500",
				},
				Cancelled: {
					completed: "bg-red-400 text-white",
					current: "bg-red-500 text-white",
					pending: "bg-red-100 text-red-600",
					border: "border-red-500",
					badge: "bg-red-200 text-red-900",
					text: "text-red-700",
					historyBorder: "border-red-500",
				},
				Archived: {
					completed: "bg-purple-400 text-white",
					current: "bg-purple-500 text-white",
					pending: "bg-purple-100 text-purple-600",
					border: "border-purple-500",
					badge: "bg-purple-200 text-purple-900",
					text: "text-purple-700",
					historyBorder: "border-purple-500",
				},
			}),
		},
	},
	data() {
		return {
			hoveredStep: null,
			expandedStep: null,
			historyGap: 8,
		};
	},
	computed: {
		isHorizontal() {
			return this.mode.startsWith("horizontal");
		},
		tooltipDirection() {
			return this.mode.split("-")[1];
		},
	},
	methods: {
		showHistory(step, index) {
			this.hoveredStep = index;
			window.addEventListener("scroll", this.updatePosition, true);
			window.addEventListener("resize", this.updatePosition);
			this.$nextTick(() => {
				this.updatePosition();
			});
		},
		hideHistory(event, index) {
			const pointerX = event?.clientX;
			const pointerY = event?.clientY;
			const stepElement = this.resolveRefElement(this.$refs?.[`step-${index}`]);
			const historyElement = this.resolveRefElement(this.$refs?.[`history-${index}`]);
			const dir = this.tooltipDirection;
			const gap = this.historyGap;
			const isInsideStepGap = this.isPointInsideElementWithMargin(
				pointerX, pointerY, stepElement,
				dir === "top" ? gap : 0,
				dir === "bottom" ? gap : 0,
				dir === "left" ? gap : 0,
				dir === "right" ? gap : 0,
			);
			const isInsideHistoryGap = this.isPointInsideElementWithMargin(
				pointerX, pointerY, historyElement,
				dir === "bottom" ? gap : 0,
				dir === "top" ? gap : 0,
				dir === "right" ? gap : 0,
				dir === "left" ? gap : 0,
			);
			if (isInsideStepGap || isInsideHistoryGap) {
				return;
			}
			this.hoveredStep = null;
			window.removeEventListener("scroll", this.updatePosition, true);
			window.removeEventListener("resize", this.updatePosition);
		},
		isPointInsideElementWithMargin(targetX, targetY, element, marginTop = 0, marginBottom = 0, marginLeft = 0, marginRight = 0) {
			if (!element) {
				return false;
			}

			const rect = element.getBoundingClientRect();
			return (
				targetX >= rect.left - marginLeft &&
				targetX <= rect.right + marginRight &&
				targetY >= rect.top - marginTop &&
				targetY <= rect.bottom + marginBottom
			);
		},
		resolveRefElement(refValue) {
			if (!refValue) {
				return null;
			}
			const candidate = Array.isArray(refValue) ? refValue[0] : refValue;
			if (candidate?.$el) {
				return candidate.$el;
			}
			return candidate;
		},
		updatePosition() {
			const inputElement = this.resolveRefElement(this.$refs?.[`step-${this.hoveredStep}`]);
			const listElement = this.resolveRefElement(this.$refs?.[`history-${this.hoveredStep}`]);

			if (inputElement && listElement) {
				const rect = inputElement.getBoundingClientRect();
				const tipRect = listElement.getBoundingClientRect();
				const gap = this.historyGap;
				let top, left;

				switch (this.tooltipDirection) {
				case "bottom":
					top = rect.bottom + window.scrollY + gap;
					left = rect.left + window.scrollX + rect.width / 2 - tipRect.width / 2;
					break;
				case "top":
					top = rect.top + window.scrollY - tipRect.height - gap;
					left = rect.left + window.scrollX + rect.width / 2 - tipRect.width / 2;
					break;
				case "left":
					top = rect.top + window.scrollY + rect.height / 2 - tipRect.height / 2;
					left = rect.left + window.scrollX - tipRect.width - gap;
					break;
				case "right":
					top = rect.top + window.scrollY + rect.height / 2 - tipRect.height / 2;
					left = rect.right + window.scrollX + gap;
					break;
				default:
					top = rect.bottom + window.scrollY + gap;
					left = rect.left + window.scrollX;
				}

				listElement.style.top = `${top}px`;
				listElement.style.left = `${left}px`;
			}
		},
		getCurrentStepColors() {
			const currentStepName = this.steps[this.currentStep]?.name;
			return this.stepColors[currentStepName];
		},
		getSegmentClass(index) {
			const colors = this.getCurrentStepColors();
			if (index < this.currentStep) {
				return colors.completed;
			} else if (index === this.currentStep) {
				return colors.current;
			} else {
				return colors.pending;
			}
		},
		getSegmentTextClass(index) {
			const colors = this.getCurrentStepColors();
			if (index < this.currentStep) {
				return colors.completed.includes("text-white") ? "text-white" : "";
			} else if (index === this.currentStep) {
				return colors.current.includes("text-white") ? "text-white" : "";
			} else {
				return "";
			}
		},
		getCurrentStepBorderClass() {
			return this.getCurrentStepColors().border;
		},
		getCurrentStepBadgeClass() {
			return this.getCurrentStepColors().badge;
		},
		getCurrentStepHistoryBorderClass() {
			return this.getCurrentStepColors().historyBorder;
		},
		getCurrentStepTextClass() {
			return this.getCurrentStepColors().text;
		},

		getStepHistory(step) { // pinia store with history or history array table
			return this.history[step.id] || null;
		},
		toggleHistory(index) {
			this.expandedStep = this.expandedStep === index ? null : index;
		},
		formatDate(date) {
			if (!date) {
				return "";
			}
			const d = new Date(date);
			return d.toLocaleDateString("fr-FR", {
				day: "2-digit",
				month: "2-digit",
				year: "numeric",
				hour: "2-digit",
				minute: "2-digit",
			});
		},
	},
};
</script>

<style scoped>
/* Animations */
.fade-enter-active,
.fade-leave-active {
	transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
	opacity: 0;
	transform: translateY(-10px);
}

.slide-enter-active,
.slide-leave-active {
	transition: all 0.3s ease;
	max-height: 1000px;
	overflow: hidden;
}

.slide-enter-from,
.slide-leave-to {
	max-height: 0;
	opacity: 0;
}

/* Scrollbar personnalisée */
.overflow-y-auto::-webkit-scrollbar {
	width: 6px;
}

.overflow-y-auto::-webkit-scrollbar-track {
	background: #f1f1f1;
	border-radius: 10px;
}

.overflow-y-auto::-webkit-scrollbar-thumb {
	background: #cbd5e0;
	border-radius: 10px;
}

.overflow-y-auto::-webkit-scrollbar-thumb:hover {
	background: #a0aec0;
}
</style>
