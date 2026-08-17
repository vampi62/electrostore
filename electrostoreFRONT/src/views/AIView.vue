<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const iaId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { useConfigsStore, useAisStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const iasStore = useAisStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (iaId.value === "new") {
		iasStore.loadToEdition(iaId.value, preset.value);
	} else {
		iasStore.setLoadingEdition(iaId.value, true);
		try {
			await iasStore.getIaById(iaId.value);
		} catch {
			delete iasStore.ais[iaId.value];
			addNotification({ message: t("ai.NotFound"), type: "error" });
			router.push("/ai");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			iasStore.getTrainStatus(iaId.value);
		}, 15000);
		iasStore.getTrainStatus(iaId.value);
		iasStore.loadToEdition(iaId.value);
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	if (intervalRefreshStatus) {
		clearInterval(intervalRefreshStatus);
	}
	iasStore.clearEdition(iaId.value);
});

let intervalRefreshStatus = null;
const iaDeleteModalShow = ref(false);
const iaSave = async() => {
	try {
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("ai.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			iasStore.setLoadingEdition(iaId.value, false);
			return;
		}
		if (iaId.value === "new") {
			const newId = await iasStore.createIa({ ...iasStore.iaEdition[iaId.value] });
			iasStore.loadToEdition(newId);
			addNotification({ message: t("ai.Created"), type: "success" });
			iaId.value = String(newId);
			router.push("/ai/" + iaId.value);
		} else {
			await iasStore.updateIa(iaId.value, { ...iasStore.iaEdition[iaId.value] });
			iasStore.loadToEdition(iaId.value);
			addNotification({ message: t("ai.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		iasStore.setLoadingEdition(iaId.value, false);
	}
};
const iaDelete = async() => {
	try {
		await iasStore.deleteIa(iaId.value);
		addNotification({ message: t("ai.Deleted"), type: "success" });
		router.push("/ai");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	iaDeleteModalShow.value = false;
};
const iaTrain = async() => {
	try {
		await iasStore.startTrain(iaId.value);
		addNotification({ message: t("ai.TrainStart"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const createSchema = () => {
	const edition = iasStore.iaEdition[iaId.value];
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.name_ia = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("ai.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("ai.NameRequired"));
	shape.description_ia = Yup.string()
		.nullable()
		.optional()
		.max(configsStore.getConfigByKey("max_length_description"), t("ai.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }));
	return Yup.object().shape(shape);
};
const labelForm = [
	{ key: "name_ia", label: "ai.Name", type: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "description_ia", label: "ai.Description", type: "textarea", rows: 4, enableCondition: "func.hasPermission([2])" },
];
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ai.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/ai',
				create: { showCondition: iaId === 'new' && authStore.hasPermission([2]), loading: iasStore.iaEdition[iaId]?.loading },
				update: { showCondition: iaId !== 'new' && authStore.hasPermission([2]), loading: iasStore.iaEdition[iaId]?.loading },
				delete: { showCondition: iaId !== 'new' && authStore.hasPermission([2]) }
			}"
			:optional-config="[
				{ label: 'ai.Train', showCondition: authStore.hasPermission([2]), loading: iasStore.status.start?.loading, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', action: iaTrain },
				{ label: 'ai.Refresh', showCondition: authStore.hasPermission([0, 1, 2]), loading: iasStore.status.train?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => iasStore.getTrainStatus(iaId) },
			]"
			@button-create="iaSave" @button-update="iaSave" @button-delete="iaDeleteModalShow = true"/>
	</div>
	<div v-if="iasStore.ais[iaId] || iaId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="iasStore.iaEdition[iaId]" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
			<StatusDisplay :data-store="iasStore.status.train" />
		</div>
	</div>
	<div v-else>
		<div>{{ $t('ai.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="iaDeleteModalShow" @close-modal="iaDeleteModalShow = false"
		:delete-action="iaDelete" :text-title="'ai.DeleteTitle'" :text-p="'ai.DeleteText'"/>
</template>
