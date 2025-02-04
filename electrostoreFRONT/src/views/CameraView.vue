<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const cameraId = route.params.id;

import { useCamerasStore, useAuthStore } from "@/stores";
const camerasStore = useCamerasStore();
const authStore = useAuthStore();

async function fetchData() {
	if (cameraId !== "new") {
		camerasStore.cameraEdition = {
			loading: false,
		};
		try {
			await camerasStore.getCameraById(cameraId);
		} catch {
			delete camerasStore.cameras[cameraId];
			addNotification({ message: "camera.VCameraUpdated", type: "error", i18n: true });
			router.push("/cameras");
			return;
		}
		camerasStore.cameraEdition = {
			loading: false,
			nom_camera: camerasStore.cameras[cameraId].nom_camera,
			url_camera: camerasStore.cameras[cameraId].url_camera,
			user_camera: camerasStore.cameras[cameraId].user_camera,
			mdp_camera: camerasStore.cameras[cameraId].mdp_camera,
		};
	} else {
		camerasStore.cameraEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	camerasStore.cameraEdition = {
		loading: false,
	};
});

const showEditModal = ref(false);
const showDeleteModal = ref(false);
const editingCamera = ref({});
const deletingCamera = ref(null);

const openEditModal = (camera) => {
	editingCamera.value = { ...camera };
	showEditModal.value = true;
};
const closeEditModal = () => {
	showEditModal.value = false;
};
const updateCamera = () => {
	camerasStore.update(editingCamera.value.id_camera, editingCamera.value);
	closeEditModal();
};
const openDeleteModal = (camera) => {
	deletingCamera.value = camera;
	showDeleteModal.value = true;
};
const closeDeleteModal = () => {
	showDeleteModal.value = false;
	deletingCamera.value = null;
};
const confirmDelete = () => {
	closeDeleteModal();
};
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('camera.VCameraTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="cameraSave" v-if="cameraId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="camerasStore.cameraEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('camera.VCameraAdd') }}</button>
			<button type="button" @click="cameraUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="camerasStore.cameraEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('camera.VCameraUpdate') }}</button>
			<button type="button" @click="cameraDeleteOpenModal" v-if="cameraId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">{{ $t('camera.VCameraDelete')
				}}</button>
			<RouterLink to="/cameras"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">{{
					$t('camera.VCameraBack') }}</RouterLink>
		</div>
	</div>

	<!-- Modal de modification -->
	<div v-if="showEditModal" @click="closeEditModal"
		class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
		<div @click.stop class="bg-white p-5 rounded-lg shadow-xl w-1/2">
			<h3 class="text-lg font-bold mb-4">Modifier la caméra</h3>
			<form @submit.prevent="updateCamera">
				<div class="mb-4">
					<label class="block text-gray-700 text-sm font-bold mb-2" for="nom">
						Nom
					</label>
					<input v-model="editingCamera.nom_camera"
						class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
						id="nom" type="text" required>
				</div>
				<div class="mb-4">
					<label class="block text-gray-700 text-sm font-bold mb-2" for="url">
						URL
					</label>
					<input v-model="editingCamera.url_camera"
						class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
						id="url" type="text" required>
				</div>
				<div class="mb-4">
					<label class="block text-gray-700 text-sm font-bold mb-2" for="login">
						Login
					</label>
					<input v-model="editingCamera.user_camera"
						class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
						id="login" type="text" required>
				</div>
				<div class="mb-4">
					<label class="block text-gray-700 text-sm font-bold mb-2" for="password">
						Password
					</label>
					<input v-model="editingCamera.mdp_camera"
						class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline"
						id="password" type="password" required>
				</div>
				<div class="flex items-center justify-between">
					<button
						class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline"
						type="submit">
						Enregistrer
					</button>
					<button @click="closeEditModal"
						class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline"
						type="button">
						Annuler
					</button>
				</div>
			</form>
		</div>
	</div>

	<!-- Modal de confirmation de suppression -->
	<div v-if="showDeleteModal" @click="closeDeleteModal"
		class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
		<div @click.stop class="bg-white p-5 rounded-lg shadow-xl">
			<h3 class="text-lg font-bold mb-4">Confirmer la suppression</h3>
			<p class="mb-4">Êtes-vous sûr de vouloir supprimer cette caméra ?</p>
			<div class="flex justify-end">
				<button @click="confirmDelete"
					class="bg-red-500 hover:bg-red-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline mr-2">
					Supprimer
				</button>
				<button @click="closeDeleteModal"
					class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline">
					Annuler
				</button>
			</div>
		</div>
	</div>
</template>
