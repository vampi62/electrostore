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

import { useConfigsStore, useIasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const iasStore = useIasStore();
const authStore = useAuthStore();

const formContainer = ref(null);

async function fetchAllData() {
	if (iaId.value === "new") {
		loadToEdition(iaId.value);
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					iasStore.iaEdition[key] = value;
				}
			});
		}
	} else {
		iasStore.iaEdition = {
			loading: true,
		};
		try {
			await iasStore.getIaById(iaId.value);
		} catch {
			delete iasStore.ias[iaId.value];
			addNotification({ message: t("ia.NotFound"), type: "error" });
			router.push("/ia");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			iasStore.getTrainStatus(iaId.value);
		}, 15000);
		iasStore.getTrainStatus(iaId.value);
		loadToEdition(iaId.value);
	}
}
function loadToEdition(id) {
	if (id === "new") {
		iasStore.iaEdition = {
			loading: false,
		};
	} else {
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
		const validationResults = await Promise.all([
			formContainer.value?.validate(),
		]);
		const allValid = validationResults.every((result) => result && result.valid);
		if (!allValid) {
			const nbErrors = validationResults.reduce((sum, result) => sum + (result ? Object.keys(result.errors).length : 0), 0);
			addNotification({
				message: t("ia.FormValidationError", { count: nbErrors }),
				type: "error",
			});
			iasStore.iaEdition.loading = false;
			return;
		}
		if (iaId.value === "new") {
			const newId = await iasStore.createIa({ ...iasStore.iaEdition });
			loadToEdition(newId);
			addNotification({ message: t("ia.Created"), type: "success" });
			iaId.value = String(newId);
			router.push("/ia/" + iaId.value);
		} else {
			await iasStore.updateIa(iaId.value, { ...iasStore.iaEdition });
			loadToEdition(iaId.value);
			addNotification({ message: t("ia.Updated"), type: "success" });
		}
	} catch (e) {
		addNotification({ message: e, type: "error" });
	} finally {
		iasStore.iaEdition.loading = false;
	}
};
const iaDelete = async() => {
	try {
		await iasStore.deleteIa(iaId.value);
		addNotification({ message: t("ia.Deleted"), type: "success" });
		router.push("/ia");
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
	iaDeleteModalShow.value = false;
};
const iaTrain = async() => {
	try {
		await iasStore.startTrain(iaId.value);
		addNotification({ message: t("ia.TrainStart"), type: "success" });
	} catch (e) {
		addNotification({ message: e, type: "error" });
	}
};
const createSchema = () => {
	const edition = iasStore.iaEdition;
	const shape = {};
	if (!edition) {
		return Yup.object().shape(shape);
	}
	shape.nom_ia = Yup.string()
		.max(configsStore.getConfigByKey("max_length_name"), t("ia.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
		.required(t("ia.NameRequired"));
	shape.description_ia = Yup.string()
		.max(configsStore.getConfigByKey("max_length_description"), t("ia.DescriptionMaxLength", { count: configsStore.getConfigByKey("max_length_description") }))
		.required(t("ia.DescriptionRequired"));
	return Yup.object().shape(shape);
};
const labelForm = [
	{ key: "nom_ia", label: "ia.Name", type: "text", enableCondition: "func.hasPermission([2])" },
	{ key: "description_ia", label: "ia.Description", type: "textarea", rows: 4, enableCondition: "func.hasPermission([2])" },
];
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('ia.Title') }}</h2>
		<TopButtonEditElement
			:main-config="{ path: '/ia',
				create: { showCondition: iaId === 'new' && authStore.hasPermission([2]), loading: iasStore.iaEdition?.loading },
				update: { showCondition: iaId !== 'new' && authStore.hasPermission([2]), loading: iasStore.iaEdition?.loading },
				delete: { showCondition: iaId !== 'new' && authStore.hasPermission([2]) }
			}"
			:optional-config="[
				{ label: 'ia.Train', showCondition: authStore.hasPermission([2]), loading: iasStore.status.start?.loading, bgColor: 'bg-green-500', hoverColor: 'hover:bg-green-600', action: iaTrain },
				{ label: 'ia.Refresh', showCondition: authStore.hasPermission([0, 1, 2]), loading: iasStore.status.train?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => iasStore.getTrainStatus(iaId) },
			]"
			@button-create="iaSave" @button-update="iaSave" @button-delete="iaDeleteModalShow = true"/>
	</div>
	<div v-if="iasStore.ias[iaId] || iaId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full space-y-4 sm:space-y-0 sm:space-x-4">
			<FormContainer ref="formContainer" :schema-builder="createSchema" :labels="labelForm" :store-data="iasStore.iaEdition" :store-user="authStore.user"
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
