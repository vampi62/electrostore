<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const iaId = ref(route.params.id);

import { useConfigsStore, useIasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const iasStore = useIasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (iaId.value === "new") {
		iasStore.iaEdition = {
			loading: false,
		};
	} else {
		iasStore.iaEdition = {
			loading: true,
		};
		try {
			await iasStore.getIaById(iaId.value);
		} catch {
			delete iasStore.ias[iaId.value];
			addNotification({ message: "ia.VIaNotFound", type: "error", i18n: true });
			router.push("/ia");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			iasStore.getTrainStatus(iaId.value);
		}, 15000);
		iasStore.getTrainStatus(iaId.value);
		iasStore.iaEdition = {
			loading: false,
			nom_ia: iasStore.ias[iaId.value].nom_ia,
			description_ia: iasStore.ias[iaId.value].description_ia,
			date_ia: iasStore.ias[iaId.value].date_ia,
			trained_ia: iasStore.ias[iaId.value].trained_ia,
		};
	}
}
onMounted(() => {
	fetchAllData();
});
onBeforeUnmount(() => {
	if (intervalRefreshStatus) {
		clearInterval(intervalRefreshStatus);
	}
	iasStore.iaEdition = {
		loading: false,
	};
});

let intervalRefreshStatus = null;
const iaDeleteModalShow = ref(false);
const iaSave = async() => {
	try {
		createSchema().validateSync(iasStore.iaEdition, { abortEarly: false });
		if (iaId.value === "new") {
			await iasStore.createIa({ ...iasStore.iaEdition });
			addNotification({ message: "ia.VIaCreated", type: "success", i18n: true });
		} else {
			await iasStore.updateIa(iaId.value, { ...iasStore.iaEdition });
			addNotification({ message: "ia.VIaUpdated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (iaId.value === "new") {
		iaId.value = String(iasStore.iaEdition.id_ia);
		router.push("/ia/" + iaId.value);
	}
};
const iaDelete = async() => {
	try {
		await iasStore.deleteIa(iaId.value);
		addNotification({ message: "ia.VIaDeleted", type: "success", i18n: true });
		router.push("/ia");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	iaDeleteModalShow.value = false;
};
const iaTrain = async() => {
	try {
		await iasStore.startTrain(iaId.value);
		addNotification({ message: "ia.VIaTrainStart", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};
const createSchema = () => {
	return Yup.object().shape({
		nom_ia: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("ia.VIaNameMaxLength") + t("common.VAllCaracters"))
			.required(t("ia.VIaNameRequired")),
		description_ia: Yup.string()
			.max(configsStore.getConfigByKey("max_length_description"), t("ia.VIaDescriptionMaxLength") + t("common.VAllCaracters"))
			.required(t("ia.VIaDescriptionRequired")),
	});
};
const labelForm = [
	{ key: "nom_ia", label: "ia.VIaName", type: "text", condition: "session.role_user === 2" },
	{ key: "description_ia", label: "ia.VIaDescription", type: "textarea", rows: 4, condition: "session.role_user === 2" },
];
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ia.VIaTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/ia', save: { roleRequired: 2, loading: iasStore.iaEdition.loading }, delete: { roleRequired: 2 } }"
			:optional-config="[
				{ label: 'ia.VIaTrain', roleRequired: 2, loading: iasStore.status.start?.loading, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', action: iaTrain },
				{ label: 'ia.VIaRefresh', roleRequired: 0, loading: iasStore.status.train?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => iasStore.getTrainStatus(iaId) },
			]"
			:id="iaId" :store-user="authStore.user" @button-save="iaSave" @button-delete="iaDeleteModalShow = true"/>
	</div>
	<div v-if="iasStore.ias[iaId] || iaId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="iasStore.iaEdition" :store-user="authStore.user"/>
			<StatusDisplay :data-store="iasStore.status.train" />
		</div>
	</div>
	<div v-else>
		<div>{{ $t('ia.VIaLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="iaDeleteModalShow" @close-modal="iaDeleteModalShow = false"
		:delete-action="iaDelete" :text-title="'ia.VIaDeleteTitle'" :text-p="'ia.VIaDeleteText'"/>
</template>
