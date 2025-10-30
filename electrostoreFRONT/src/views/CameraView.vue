<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const cameraId = ref(route.params.id);

import { useConfigsStore, useCamerasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (cameraId.value !== "new") {
		camerasStore.cameraEdition = {
			loading: true,
		};
		try {
			await camerasStore.getCameraById(cameraId.value);
		} catch {
			delete camerasStore.cameras[cameraId.value];
			addNotification({ message: "camera.VCameraNotFound", type: "error", i18n: true });
			router.push("/cameras");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			camerasStore.getStatus(cameraId.value);
		}, 15000);
		camerasStore.getStatus(cameraId.value);
		camerasStore.getStream(cameraId.value);
		camerasStore.cameraEdition = {
			loading: false,
			nom_camera: camerasStore.cameras[cameraId.value].nom_camera,
			url_camera: camerasStore.cameras[cameraId.value].url_camera,
			user_camera: camerasStore.cameras[cameraId.value].user_camera,
			mdp_camera: camerasStore.cameras[cameraId.value].mdp_camera,
		};
		isChecked.value = (camerasStore.cameras[cameraId.value].user_camera !== "") || (camerasStore.cameras[cameraId.value].mdp_camera !== "");
	} else {
		camerasStore.cameraEdition = {
			loading: false,
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
	if (camerasStore.stream[cameraId.value]) {
		delete camerasStore.stream[cameraId.value];
	}
	camerasStore.cameraEdition = {
		loading: false,
	};
});

let intervalRefreshStatus = null;
const cameraDeleteModalShow = ref(false);
const cameraSave = async() => {
	if (!isChecked.value) {
		camerasStore.cameraEdition.user_camera = "";
		camerasStore.cameraEdition.mdp_camera = "";
	}
	try {
		createSchema(isChecked).validateSync(camerasStore.cameraEdition, { abortEarly: false });
		if (cameraId.value !== "new") {
			await camerasStore.updateCamera(cameraId.value, { ...camerasStore.cameraEdition });
			camerasStore.getStatus(cameraId.value);
			camerasStore.getStream(cameraId.value);
			addNotification({ message: "camera.VCameraUpdated", type: "success", i18n: true });
		} else {
			await camerasStore.createCamera({ ...camerasStore.cameraEdition } );
			addNotification({ message: "camera.VCameraCreated", type: "success", i18n: true });
		}
	} catch (e) {
		if (e.inner) {
			e.inner.forEach((error) => {
				addNotification({ message: error.message, type: "error", i18n: false });
			});
			return;
		}
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
	if (cameraId.value === "new") {
		cameraId.value = String(camerasStore.cameraEdition.id_camera);
		router.push("/cameras/" + cameraId.value);
	}
};
const cameraDelete = async() => {
	try {
		await camerasStore.deleteCamera(cameraId.value);
		addNotification({ message: "camera.VCameraDeleted", type: "success", i18n: true });
		router.push("/cameras");
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
	}
	cameraDeleteModalShow.value = false;
};
const cameraUpdateLight = async(id) => {
	await camerasStore.toggleLight(id);
	camerasStore.getStatus(id);
};

const isChecked = ref(false);
const createSchema = (isChecked) => {
	return Yup.object().shape({
		nom_camera: Yup.string()
			.max(configsStore.getConfigByKey("max_length_name"), t("camera.VCameraNameMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			.required(t("camera.VCameraNameRequired")),
		url_camera: Yup.string()
			.max(configsStore.getConfigByKey("max_length_url"), t("camera.VCameraURLMaxLength") + " " + configsStore.getConfigByKey("max_length_url") + t("common.VAllCaracters"))
			.required(t("camera.VCameraDescriptionRequired")),
		user_camera: isChecked
			? Yup.string().required(t("camera.VCameraUserRequired")).max(configsStore.getConfigByKey("max_length_name"), t("camera.VCameraUserMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			: Yup.string().nullable(),
		mdp_camera: isChecked
			? Yup.string().required(t("camera.VCameraPasswordRequired")).max(configsStore.getConfigByKey("max_length_name"), t("camera.VCameraPasswordMaxLength") + " " + configsStore.getConfigByKey("max_length_name") + t("common.VAllCaracters"))
			: Yup.string().nullable(),
	});
};

const labelForm = ref([
	{ key: "nom_camera", label: "camera.VCameraName", type: "text", condition: "session.role_user === 2" },
	{ key: "url_camera", label: "camera.VCameraURL", type: "text", condition: "session.role_user === 2" },
	{ key: "check", label: "camera.VCameraCheck", type: "checkbox", model: isChecked, condition: "session.role_user === 2" },
	{ key: "user_camera", label: "camera.VCameraUser", type: "text", condition: "session.role_user === 2 && form[2].model" },
	{ key: "mdp_camera", label: "camera.VCameraPassword", type: "password", condition: "session.role_user === 2 && form[2].model" },
]);
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('camera.VCameraTitle') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/cameras', save: { roleRequired: 2, loading: camerasStore.cameraEdition.loading }, delete: { roleRequired: 2 } }"
			:optional-config="[
				{ label: 'camera.VCameraOnOff', roleRequired: 2, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => cameraUpdateLight(cameraId) },
				{ label: 'camera.VCameraRefresh', roleRequired: 2, loading: camerasStore.status[cameraId]?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => camerasStore.getStatus(cameraId) }
			]"
			:id="cameraId" :store-user="authStore.user" @button-save="cameraSave" @button-delete="cameraDeleteModalShow = true"/>
	</div>
	<div v-if="camerasStore.cameras[cameraId] || cameraId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full gap-y-4 gap-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="camerasStore.cameraEdition" :store-user="authStore.user"/>
			<div class="flex-1 min-w-64 min-h-80 bg-gray-200 px-4 py-2 rounded">
				<img :src="camerasStore.stream[cameraId]" alt="Camera Stream" />
			</div>
			<StatusDisplay :data-store="camerasStore.status[cameraId]" />
		</div>
	</div>
	<div v-else>
		<div>{{ $t('camera.VCameraLoading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="cameraDeleteModalShow" @close-modal="cameraDeleteModalShow = false"
		@delete-confirmed="cameraDelete" :text-title="'camera.VCameraDeleteTitle'" :text-p="'camera.VCameraDeleteText'"/>
</template>
