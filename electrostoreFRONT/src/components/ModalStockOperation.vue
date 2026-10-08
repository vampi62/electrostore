<script setup lang="ts">
import { ref, computed, watch, inject } from "vue";
import { useI18n } from "vue-i18n";

import type { useNotification } from "@/composables";
import { loadLedPreferences, toShowQuery } from "@/utils";
import type { LedOperation } from "@/utils";
import { useItemsStore, useStoresStore, useAuthStore } from "@/stores";

type PlanStep = { idBox: number; idStore: number; current: number; change: number; max: number };

const props = defineProps<{ showModal: boolean; itemId: string }>();
const emit = defineEmits<{ (e: "closeModal"): void }>();

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const itemsStore = useItemsStore();
const storesStore = useStoresStore();
const authStore = useAuthStore();

const operation = ref<LedOperation>("remove");
const quantity = ref(1);
const plan = ref<PlanStep[]>([]);
const stepIndex = ref(-1);
const loading = ref(false);

const running = computed(() => stepIndex.value >= 0 && stepIndex.value < plan.value.length);
const currentStep = computed(() => (running.value ? plan.value[stepIndex.value] : null));

watch(() => props.showModal, (show) => {
	if (show) {
		plan.value = [];
		stepIndex.value = -1;
		quantity.value = 1;
	}
});

const buildPlan = (): PlanStep[] | null => {
	const boxs = (Object.values(itemsStore.itemBoxs[props.itemId] || {}) as any[]).map((ib) => ({
		idBox: ib.id_box,
		idStore: ib.box?.id_store ?? ib.id_store,
		current: ib.quantity_item_box,
		max: ib.threshold_max_item_item_box,
	}));
	// the fullest boxes first so that the number of boxes to open stays minimal
	const candidates = boxs
		.filter((b) => (operation.value === "remove" ? b.current > 0 : b.max - b.current > 0))
		.sort((a, b) => b.current - a.current);
	let left = quantity.value;
	const steps: PlanStep[] = [];
	for (const box of candidates) {
		if (left <= 0) {
			break;
		}
		const available = operation.value === "remove" ? box.current : box.max - box.current;
		const change = Math.min(left, available);
		steps.push({ ...box, change });
		left -= change;
	}
	return left > 0 ? null : steps;
};

const showStep = async(step: PlanStep) => {
	try {
		const prefs = loadLedPreferences(authStore.user?.id_user)[operation.value];
		await storesStore.showBoxById(String(step.idStore), String(step.idBox), toShowQuery(prefs));
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};

const start = async() => {
	if (!Number.isInteger(quantity.value) || quantity.value < 1) {
		addNotification({ message: t("item.StockQuantityInvalid"), type: "error" });
		return;
	}
	loading.value = true;
	try {
		await itemsStore.getItemBoxByInterval(props.itemId, 100, 0, ["box"], [], {}, true);
		const steps = buildPlan();
		if (!steps) {
			addNotification({ message: t(operation.value === "remove" ? "item.StockNotEnough" : "item.StockNoRoom"), type: "error" });
			return;
		}
		plan.value = steps;
		stepIndex.value = 0;
		await showStep(steps[0]);
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		loading.value = false;
	}
};

const confirmStep = async() => {
	const step = currentStep.value;
	if (!step) {
		return;
	}
	loading.value = true;
	try {
		const newQuantity = step.current + (operation.value === "remove" ? -step.change : step.change);
		await itemsStore.updateItemBox(props.itemId, step.idBox, { quantity_item_box: newQuantity, threshold_max_item_item_box: step.max });
		stepIndex.value++;
		if (running.value) {
			await showStep(plan.value[stepIndex.value]);
		} else {
			addNotification({ message: t("item.StockOperationDone"), type: "success" });
			emit("closeModal");
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		loading.value = false;
	}
};
</script>

<template>
	<div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center z-50"
		@click="emit('closeModal')">
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
						class="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50">
						{{ $t('item.StockStart') }}
					</button>
					<button type="button" @click="emit('closeModal')"
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
						class="px-4 py-2 bg-yellow-500 text-white rounded-lg hover:bg-yellow-600 disabled:opacity-50">
						<i class="fa-solid fa-eye"></i>
					</button>
					<button type="button" @click="confirmStep" :disabled="loading"
						class="px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:opacity-50">
						{{ $t('item.StockConfirm') }}
					</button>
					<button type="button" @click="emit('closeModal')"
						class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
						{{ $t('components.VModalDeleteCancel') }}
					</button>
				</div>
			</template>
		</div>
	</div>
</template>
