<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="$emit('closeModal')">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('item.StockOperationTitle') }}</h2>
			<template v-if="!running">
				<div class="flex space-x-4 mb-4">
					<label class="flex items-center space-x-1">
						<input type="radio" value="remove" v-model="operation" />
						<span>{{ $t('item.StockRemove') }}</span>
					</label>
					<label class="flex items-center space-x-1">
						<input type="radio" value="add" v-model="operation" />
						<span>{{ $t('item.StockAdd') }}</span>
					</label>
				</div>
				<label class="flex items-center space-x-4 mb-4">
					<span>{{ $t('item.StockQuantity') }}</span>
					<input type="number" min="1" step="1" v-model.number="quantity" class="w-24 border rounded px-2 py-1" />
				</label>
				<div class="flex justify-end space-x-4">
					<button type="button" @click="start" :disabled="loading"
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600">
						{{ $t('item.StockStart') }}
					</button>
					<button type="button" @click="$emit('closeModal')"
						class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
						{{ $t('components.VModalDeleteCancel') }}
					</button>
				</div>
			</template>
			<template v-else-if="currentStep">
				<p class="text-sm text-gray-500 mb-2">{{ $t('item.StockStep', { current: stepIndex + 1, total: plan.length }) }}</p>
				<p class="text-lg mb-4">
					{{ $t(operation === 'remove' ? 'item.StockTake' : 'item.StockPut', { count: currentStep.change, box: currentStep.idBox }) }}
				</p>
				<div class="flex justify-end space-x-4">
					<button type="button" @click="showStep(currentStep)" :disabled="loading"
						class="px-4 py-2 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600">
						<font-awesome-icon icon="fa-solid fa-eye" />
					</button>
					<button type="button" @click="confirmStep" :disabled="loading"
						class="px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600">
						{{ $t('item.StockConfirm') }}
					</button>
					<button type="button" @click="$emit('closeModal')"
						class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
						{{ $t('components.VModalDeleteCancel') }}
					</button>
				</div>
			</template>
		</div>
	</div>
</template>

<script lang="ts">
import { inject } from "vue";
import { useI18n } from "vue-i18n";
import type { useNotification } from "@/composables";
import { loadLedPreferences, toShowQuery } from "@/utils";
import type { LedOperation } from "@/utils";
import { useItemsBoxsStore, useStoresStore, useAuthStore } from "@/stores";
type PlanStep = { idBox: number; idStore: number; current: number; change: number; max: number };
export default {
	name: "ModalStockOperation",
	props: {
		showModal: {
			type: Boolean,
			required: true,
		},
		itemId: {
			type: String,
			required: true,
		},
	},
	emits: ["closeModal"],
	data() {
		return {
			operation: "remove" as LedOperation,
			quantity: 1,
			plan: [] as PlanStep[],
			stepIndex: -1,
			loading: false,
		};
	},
	setup() {
		const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
		const { t } = useI18n();
		const itemBoxsStore = useItemsBoxsStore();
		const storesStore = useStoresStore();
		const authStore = useAuthStore();
		return { 
			addNotification,
			t,
			itemBoxsStore,
			storesStore,
			authStore,
		};
	},
	computed: {
		running() {
			return this.stepIndex >= 0 && this.stepIndex < this.plan.length;
		},
		currentStep() {
			return this.running ? this.plan[this.stepIndex] : null;
		},
	},
	watch: {
		showModal(show: boolean) {
			if (show) {
				this.plan = [];
				this.stepIndex = -1;
				this.quantity = 1;
			}
		},
	},
	methods: {
		buildPlan() {
			const boxs = (Object.values(this.itemBoxsStore.itemBoxsByItem(this.itemId)) as any[]).map((ib) => ({
				idBox: ib.id_box,
				idStore: ib.box?.id_store ?? ib.id_store,
				current: ib.quantity_item_box,
				max: ib.threshold_max_item_item_box,
			}));
			// the fullest boxes first so that the number of boxes to open stays minimal
			const candidates = boxs
				.filter((b) => (this.operation === "remove" ? b.current > 0 : b.max - b.current > 0))
				.sort((a, b) => b.current - a.current);
			let left = this.quantity;
			const steps: PlanStep[] = [];
			for (const box of candidates) {
				if (left <= 0) {
					break;
				}
				const available = this.operation === "remove" ? box.current : box.max - box.current;
				const change = Math.min(left, available);
				steps.push({ ...box, change });
				left -= change;
			}
			return left > 0 ? null : steps;
		},
		async showStep(step: PlanStep) {
			try {
				const prefs = loadLedPreferences(this.authStore.user?.id_user)[this.operation];
				return this.storesStore.showBoxById(String(step.idStore), String(step.idBox), toShowQuery(prefs));
			} catch (e) {
				this.addNotification({ message: e, type: "error" });
			}
		},
		async start() {
			if (!Number.isInteger(this.quantity) || this.quantity < 1) {
				this.addNotification({ message: this.t("item.StockQuantityInvalid"), type: "error" });
				return;
			}
			this.loading = true;
			try {
				await this.itemBoxsStore.getItemBoxByInterval({ id_item: this.itemId }, 100, 0, ["box"], [], {}, true);
				const steps = this.buildPlan();
				if (!steps) {
					this.addNotification({ message: this.t(this.operation === "remove" ? "item.StockNotEnough" : "item.StockNoRoom"), type: "error" });
					return;
				}
				this.plan = steps;
				this.stepIndex = 0;
				await this.showStep(steps[0]);
			} catch (e) {
				this.addNotification({ message: e, type: "error" });
			} finally {
				this.loading = false;
			}
		},
		async confirmStep() {
			const step = this.plan[this.stepIndex];
			if (!step) {
				return;
			}
			this.loading = true;
			try {
				const newQuantity = step.current + (this.operation === "remove" ? -step.change : step.change);
				await this.itemBoxsStore.updateItemBox({ id_item: this.itemId, id_box: String(step.idBox) }, { quantity_item_box: newQuantity, threshold_max_item_item_box: step.max });
				this.stepIndex++;
				if (this.stepIndex < this.plan.length) {
					await this.showStep(this.plan[this.stepIndex]);
				} else {
					this.addNotification({ message: this.t("item.StockOperationDone"), type: "success" });
					this.$emit("closeModal");
				}
			} catch (e) {
				this.addNotification({ message: e, type: "error" });
			} finally {
				this.loading = false;
			}
		},
	},
};
</script>