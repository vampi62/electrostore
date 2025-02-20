<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject, watch } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useRoute } from "vue-router";
const route = useRoute();
const cameraId = route.params.id;

import { useConfigsStore, useCamerasStore, useAuthStore } from "@/stores";
const configsStore = useConfigsStore();
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchAllData() {
	if (cameraId !== "new") {
		camerasStore.cameraEdition = {
			loading: true,
		};
		try {
			await camerasStore.getCameraById(cameraId);
		} catch {
			delete camerasStore.cameras[cameraId];
			addNotification({ message: "camera.VCameraNotFound", type: "error", i18n: true });
			router.push("/cameras");
			return;
		}
		intervalRefreshStatus = setInterval(() => {
			camerasStore.getStatus(cameraId);
		}, 15000);
		camerasStore.getStatus(cameraId);
		camerasStore.getStream(cameraId);
		camerasStore.cameraEdition = {
			loading: false,
			nom_camera: camerasStore.cameras[cameraId].nom_camera,
			url_camera: camerasStore.cameras[cameraId].url_camera,
			user_camera: camerasStore.cameras[cameraId].user_camera,
			mdp_camera: camerasStore.cameras[cameraId].mdp_camera,
		};
		isChecked.value = (camerasStore.cameras[cameraId].user_camera !== "") || (camerasStore.cameras[cameraId].mdp_camera !== "");
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
	if (camerasStore.stream[cameraId]) {
		delete camerasStore.stream[cameraId];
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
		await schemaCamera.value.validate(camerasStore.cameraEdition, { abortEarly: false });
		await camerasStore.createCamera(camerasStore.cameraEdition);
		addNotification({ message: "camera.VCameraCreated", type: "success", i18n: true });
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
	cameraId = camerasStore.cameraEdition.id_camera;
	router.push("/cameras/" + camerasStore.cameraEdition.id_camera);
};
const cameraUpdate = async() => {
	if (!isChecked.value) {
		camerasStore.cameraEdition.user_camera = "";
		camerasStore.cameraEdition.mdp_camera = "";
	}
	try {
		await schemaCamera.value.validate(camerasStore.cameraEdition, { abortEarly: false });
		await camerasStore.updateCamera(cameraId, camerasStore.cameraEdition);
		addNotification({ message: "camera.VCameraUpdated", type: "success", i18n: true });
		camerasStore.getStatus(cameraId);
		camerasStore.getStream(cameraId);
	} catch (e) {
		e.inner.forEach((error) => {
			addNotification({ message: error.message, type: "error", i18n: false });
		});
		return;
	}
};
const cameraDelete = async() => {
	try {
		await camerasStore.deleteCamera(cameraId);
		addNotification({ message: "camera.VCameraDeleted", type: "success", i18n: true });
		router.push("/cameras");
	} catch (e) {
		addNotification({ message: "camera.VCameraDeleteError", type: "error", i18n: true });
	}
	cameraDeleteModalShow.value = false;
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
const schemaCamera = ref(createSchema(camerasStore.cameraEdition.is_checked_custom));
watch(isChecked, (newValue) => {
	schemaCamera.value = createSchema(newValue);
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('camera.VCameraTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="camerasStore.toggleLight(cameraId)" v-if="cameraId != 'new'"
				class="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 flex items-center">
				{{ $t('camera.VCameraOnOff') }}
			</button>
			<button type="button" @click="camerasStore.getStatus(cameraId)" v-if="cameraId != 'new'"
				class="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600 flex items-center">
				<span v-show="camerasStore.status[cameraId]?.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('camera.VCameraRefresh') }}
			</button>
			<button type="button" @click="cameraSave" v-if="cameraId == 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="camerasStore.cameraEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('camera.VCameraAdd') }}
			</button>
			<button type="button" @click="cameraUpdate" v-else-if="cameraId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="camerasStore.cameraEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('camera.VCameraUpdate') }}
			</button>
			<button type="button" @click="cameraDeleteModalShow = true" v-if="cameraId != 'new' && authStore.user?.role_user == 'admin'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('camera.VCameraDelete') }}
			</button>
			<RouterLink to="/cameras" class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('camera.VCameraBack') }}
			</RouterLink>
		</div>
	</div>
	<div v-if="camerasStore.cameras[cameraId] || cameraId == 'new'">
		<div class="mb-6 flex justify-between flex-wrap">
			<Form :validation-schema="schemaCamera" v-slot="{ errors }" @submit.prevent="" class="mb-6">
				<table class="table-auto text-gray-700">
					<tbody>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('camera.VCameraName') }}:</td>
							<td class="flex flex-col">
								<Field name="nom_camera" type="text" v-model="camerasStore.cameraEdition.nom_camera"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.nom_camera }"
									:disabled="authStore.user?.role_user !== 'admin'" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.nom_camera || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('camera.VCameraURL') }}:</td>
							<td class="flex flex-col">
								<Field name="url_camera" type="text" v-model="camerasStore.cameraEdition.url_camera"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.url_camera }"
									:disabled="authStore.user?.role_user !== 'admin'" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.url_camera || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('camera.VCameraCheck') }}:</td>
							<td class="flex flex-col">
								<Field name="check" v-slot="{ is_checked_custom }">
									<input
										v-model="isChecked"
										v-bind="is_checked_custom"
										type="checkbox"
										:checked="isChecked"
										class="form-checkbox h-5 w-5 text-blue-600"
									/>
								</Field>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('camera.VCameraUser') }}:</td>
							<td class="flex flex-col">
								<Field name="user_camera" type="text" v-model="camerasStore.cameraEdition.user_camera"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.user_camera }"
									:disabled="authStore.user?.role_user !== 'admin' || !isChecked" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.user_camera || ' ' }}</span>
							</td>
						</tr>
						<tr>
							<td class="font-semibold pr-4 align-text-top">{{ $t('camera.VCameraPassword') }}:</td>
							<td class="flex flex-col">
								<Field name="mdp_camera" type="password" v-model="camerasStore.cameraEdition.mdp_camera"
									class="border border-gray-300 rounded px-2 py-1 w-full focus:outline-none focus:ring focus:ring-blue-300"
									:class="{ 'border-red-500': errors.mdp_camera }"
									:disabled="authStore.user?.role_user !== 'admin' || !isChecked" />
								<span class="text-red-500 h-5 w-80 text-sm">{{ errors.mdp_camera || ' ' }}</span>
							</td>
						</tr>
					</tbody>
				</table>
			</Form>
			<div class="w-96 h-80 bg-gray-200 px-4 py-2 rounded">
				<img :src=camerasStore.stream[cameraId] />
			</div>
			<div class="w-96 h-80 bg-gray-200 px-4 py-2 rounded">
				<div v-for="(value, key) in camerasStore.status[cameraId]" :key="key">
					<div v-if="key !== 'loading' ">
						{{ key }}: {{ value }}
					</div>
				</div>
			</div>
		</div>
	</div>
	<div v-else>
		<div>{{ $t('camera.VCameraLoading') }}</div>
	</div>

	<div v-if="cameraDeleteModalShow" class="fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center"
		@click="cameraDeleteModalShow = false">
		<div class="bg-white p-6 rounded shadow-lg w-96" @click.stop>
			<h2 class="text-xl mb-4">{{ $t('camera.VCameraDeleteTitle') }}</h2>
			<p>{{ $t('camera.VCameraDeleteText') }}</p>
			<div class="flex justify-end space-x-4 mt-4">
				<button type="button" @click="cameraDelete()"
					class="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600">
					{{ $t('camera.VCameraDeleteConfirm') }}
				</button>
				<button type="button" @click="cameraDeleteModalShow = false"
					class="px-4 py-2 bg-gray-400 text-white rounded-lg hover:bg-gray-500">
					{{ $t('camera.VCameraDeleteCancel') }}
				</button>
			</div>
		</div>
	</div>
</template>
