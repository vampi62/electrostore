<script setup>
import { onMounted, onBeforeUnmount, ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const cameraId = ref(route.params.id);
const preset = ref(route.query.preset || null);

import { useConfigsStore, useCamerasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (cameraId.value === "new") {
		camerasStore.cameraEdition = {
			loading: false,
		};
		if (preset.value) {
			preset.value.split(";").forEach((pair) => {
				const [key, value] = pair.split(":");
				if (key && value) {
					camerasStore.cameraEdition[key] = value;
				}
			});
		}
	} else {
		camerasStore.cameraEdition = {
			loading: true,
		};
		try {
			await camerasStore.getCameraById(cameraId.value);
		} catch {
			delete camerasStore.cameras[cameraId.value];
			addNotification({ message: "camera.NotFound", type: "error", i18n: true });
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
		if (cameraId.value === "new") {
			const newId = await camerasStore.createCamera({ ...camerasStore.cameraEdition } );
			addNotification({ message: "camera.Created", type: "success", i18n: true });
			cameraId.value = String(newId);
			router.push("/cameras/" + cameraId.value);
		} else {
			await camerasStore.updateCamera(cameraId.value, { ...camerasStore.cameraEdition });
			camerasStore.getStatus(cameraId.value);
			camerasStore.getStream(cameraId.value);
			addNotification({ message: "camera.Updated", type: "success", i18n: true });
		}
	} catch (e) {
		addNotification({ message: e, type: "error", i18n: false });
		return;
	}
};
const cameraDelete = async() => {
	try {
		await camerasStore.deleteCamera(cameraId.value);
		addNotification({ message: "camera.Deleted", type: "success", i18n: true });
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
			.max(configsStore.getConfigByKey("max_length_name"), t("camera.NameMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			.required(t("camera.NameRequired")),
		url_camera: Yup.string()
			.max(configsStore.getConfigByKey("max_length_url"), t("camera.URLMaxLength", { count: configsStore.getConfigByKey("max_length_url") }))
			.required(t("camera.DescriptionRequired")),
		user_camera: isChecked
			? Yup.string().required(t("camera.UserRequired")).max(configsStore.getConfigByKey("max_length_name"), t("camera.UserMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			: Yup.string().nullable(),
		mdp_camera: isChecked
			? Yup.string().required(t("camera.PasswordRequired")).max(configsStore.getConfigByKey("max_length_name"), t("camera.PasswordMaxLength", { count: configsStore.getConfigByKey("max_length_name") }))
			: Yup.string().nullable(),
	});
};

const labelForm = ref([
	{ key: "nom_camera", label: "camera.Name", type: "text", condition: "func.hasPermission([2])" },
	{ key: "url_camera", label: "camera.URL", type: "text", condition: "func.hasPermission([2])" },
	{ key: "check", label: "camera.Check", type: "checkbox", model: isChecked, condition: "func.hasPermission([2])" },
	{ key: "user_camera", label: "camera.User", type: "text", condition: "func.hasPermission([2]) && form[2].model" },
	{ key: "mdp_camera", label: "camera.Password", type: "password", condition: "func.hasPermission([2]) && form[2].model" },
]);
document.querySelector("#view").classList.add("overflow-y-scroll");
</script>
<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('camera.Title') }}</h2>
		<TopButtonEditElement :main-config="{ path: '/cameras', save: { roleRequired: authStore.hasPermission([2]), loading: camerasStore.cameraEdition.loading }, delete: { roleRequired: authStore.hasPermission([2]) } }"
			:optional-config="[
				{ label: 'camera.OnOff', roleRequired: authStore.hasPermission([2]), bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => cameraUpdateLight(cameraId) },
				{ label: 'camera.Refresh', roleRequired: authStore.hasPermission([2]), loading: camerasStore.status[cameraId]?.loading, bgColor: 'bg-gray-500', hoverColor: 'hover:bg-gray-600', action: () => camerasStore.getStatus(cameraId) }
			]"
			:id="cameraId" @button-save="cameraSave" @button-delete="cameraDeleteModalShow = true"/>
	</div>
	<div v-if="camerasStore.cameras[cameraId] || cameraId == 'new'" class="w-full">
		<div class="mb-6 flex justify-between flex-wrap w-full gap-y-4 gap-x-4">
			<FormContainer :schema-builder="createSchema" :labels="labelForm" :store-data="camerasStore.cameraEdition" :store-user="authStore.user"
				:store-function="{ hasPermission: (validPerm) => authStore.hasPermission(validPerm) }"/>
			<div class="flex-1 min-w-64 min-h-80 bg-gray-200 px-4 py-2 rounded">
				<img :src="camerasStore.stream[cameraId]" alt="Camera Stream" />
			</div>
			<StatusDisplay :data-store="camerasStore.status[cameraId]" />
		</div>
	</div>
	<div v-else>
		<div>{{ $t('camera.Loading') }}</div>
	</div>

	<ModalDeleteConfirm :show-modal="cameraDeleteModalShow" @close-modal="cameraDeleteModalShow = false"
		:delete-action="cameraDelete" :text-title="'camera.DeleteTitle'" :text-p="'camera.DeleteText'"/>
</template>
