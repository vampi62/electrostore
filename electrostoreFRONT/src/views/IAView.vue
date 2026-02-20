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

import { useConfigsStore, useIasStore } from "@/stores";
const configsStore = useConfigsStore();
const iasStore = useIasStore();

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
			addNotification({ message: "ia.NotFound", type: "error", i18n: true });
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
			addNotification({ message: "ia.Created", type: "success", i18n: true });
		} else {
			await iasStore.updateIa(iaId.value, { ...iasStore.iaEdition });
			addNotification({ message: "ia.Updated", type: "success", i18n: true });
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
		addNotification({ message: "ia.Deleted", type: "success", i18n: true });
		router.push("/ia");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	iaDeleteModalShow.value = false;
};
const iaTrain = async() => {
	try {
		await iasStore.startTrain(iaId.value);
		addNotification({ message: "ia.TrainStart", type: "success", i18n: true });
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
};
const createSchema = () => {
	return Yup.object().shape({
		nom_ia: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("ia.NameMaxLength") + t("common.VAllCaracters"))
			.required(t("ia.NameRequired")),
		description_ia: Yup.string()
			.max(configsStore.getConfigByKey("max_length_description"), t("ia.DescriptionMaxLength") + t("common.VAllCaracters"))
			.required(t("ia.DescriptionRequired")),
	});
};
const labelForm = [
	{ key: "nom_ia", label: "ia.Name", type: "text", condition: "func.hasPermission([2])" },
	{ key: "description_ia", label: "ia.Description", type: "textarea", rows: 4, condition: "func.hasPermission([2])" },
];
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ia.Title') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/ia', save: { roleRequired: authStore.hasPermission([2]), loading: iasStore.iaEdition.loading }, delete: { roleRequired: authStore.hasPermission([2]) } }"
			:optional-config="[
				{ label: 'ia.Train', roleRequired: authStore.hasPermission([2]), loading: iasStore.status.start?.loading, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', action: iaTrain },
				{ label: 'ia.Refresh', roleRequired: authStore.hasPermission([0, 1, 2]), loading: iasStore.status.train?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => iasStore.getTrainStatus(iaId) },
			]"
			:id="iaId" :store-user="authStore.user" @button-save="iaSave" @button-delete="iaDeleteModalShow = true"/>
	</div>
	<div v-if="iasStore.ias[iaId] || iaId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="iasStore.iaEdition" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
			<StatusDisplay :data-store="iasStore.status.train" />
		</div>
	</div>
	<div v-else>
		<div>{{ $t('ia.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="iaDeleteModalShow" @close-modal="iaDeleteModalShow = false"
		:delete-action="iaDelete" :text-title="'ia.DeleteTitle'" :text-p="'ia.DeleteText'"/>
</template>
