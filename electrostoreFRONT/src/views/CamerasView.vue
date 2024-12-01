<script setup>
import { useCamerasStore } from '@/stores';
const camerasStore = useCamerasStore();
camerasStore.getAll();

import { ref } from 'vue';

const showEditModal = ref(false);
const showDeleteModal = ref(false);
const editingCamera = ref({});
const deletingCamera = ref(null);


const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const openEditModal = (camera) => {
  editingCamera.value = { ...camera };
  showEditModal.value = true;
};
const closeEditModal = () => {
  showEditModal.value = false;
};
const updateCamera = () => {
  // Logique pour mettre à jour la caméra
  console.log('Mise à jour de la caméra:', editingCamera.value);
  camerasStore.update(editingCamera.value.id_camera, editingCamera.value)
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
  // Logique pour supprimer la caméra
  console.log('Suppression de la caméra:', deletingCamera.value);
  closeDeleteModal();
};
const toggleCamera = (camera) => {
  if (camerasStore.status[camera.id_camera]?.ringLightPower > 0) {
    camerasStore.toggleLight(camera.id_camera, false);
  } else {
    camerasStore.toggleLight(camera.id_camera, true);
  }
  // waiting 0.5s
  sleep(100);
  camerasStore.getStatus(camera.id_camera);
};
const streamCamera = (camera) => {
    camerasStore.getStream(camera.id_camera);
};
const captureCamera = (camera) => {
    camerasStore.getCapture(camera.id_camera);
};
const stopStream = (camera) => {
    camerasStore.stopStream(camera.id_camera);
};
</script>

<template>
    <div>
        <h2>camera</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">url</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">login</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">password</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">toggle</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">stream</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">capture</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!camerasStore.cameras.loading">
                <tr v-for="camera in camerasStore.cameras" :key="camera.id_camera">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.id_camera }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.nom_camera }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.url_camera }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.user_camera }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.mdp_camera }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button @click="openEditModal(camera)" class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
                            Modifier
                        </button>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button @click="openDeleteModal(camera)" class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                            Supprimer
                        </button>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button @click="toggleCamera(camera)" class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                            toggle
                        </button>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        
                        <div v-if="!camerasStore.stream[camera.id_camera]">
                            <button @click="streamCamera(camera)" class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                                enable
                            </button>
                        </div>
                        <div v-else>
                            <button @click="camerasStore.stopStream(camera.id_camera)" class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                                disable
                            </button>
                        </div>
                        <div v-if="camerasStore.stream[camera.id_camera]">
                            <img alt="Image provenant d'un flux vidéo" :src="camerasStore.stream[camera.id_camera]" />
                        </div>
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                        <button @click="captureCamera(camera)" class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                            capture
                        </button>
                        <div v-if="camerasStore.capture[camera.id_camera]">
                            <img alt="Image provenant d'un flux vidéo" :src="camerasStore.capture[camera.id_camera]"  class="w-16 h-16 object-cover rounded"/>
                        </div>
                    </td>
                </tr>
            </template>
        </tbody>
    </table>

    <!-- Modal de modification -->
    <div v-if="showEditModal" @click="closeEditModal" class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
        <div @click.stop class="bg-white p-5 rounded-lg shadow-xl w-1/2">
            <h3 class="text-lg font-bold mb-4">Modifier la caméra</h3>
            <form @submit.prevent="updateCamera">
                <div class="mb-4">
                    <label class="block text-gray-700 text-sm font-bold mb-2" for="nom">
                        Nom
                    </label>
                    <input v-model="editingCamera.nom_camera" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline" id="nom" type="text" required>
                </div>
                <div class="mb-4">
                    <label class="block text-gray-700 text-sm font-bold mb-2" for="url">
                        URL
                    </label>
                    <input v-model="editingCamera.url_camera" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline" id="url" type="text" required>
                </div>
                <div class="mb-4">
                    <label class="block text-gray-700 text-sm font-bold mb-2" for="login">
                        Login
                    </label>
                    <input v-model="editingCamera.user_camera" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline" id="login" type="text" required>
                </div>
                <div class="mb-4">
                    <label class="block text-gray-700 text-sm font-bold mb-2" for="password">
                        Password
                    </label>
                    <input v-model="editingCamera.mdp_camera" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:shadow-outline" id="password" type="password" required>
                </div>
                <div class="flex items-center justify-between">
                    <button class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline" type="submit">
                        Enregistrer
                    </button>
                    <button @click="closeEditModal" class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline" type="button">
                        Annuler
                    </button>
                </div>
            </form>
        </div>
    </div>

        <!-- Modal de confirmation de suppression -->
    <div v-if="showDeleteModal" @click="closeDeleteModal" class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
        <div @click.stop class="bg-white p-5 rounded-lg shadow-xl">
            <h3 class="text-lg font-bold mb-4">Confirmer la suppression</h3>
            <p class="mb-4">Êtes-vous sûr de vouloir supprimer cette caméra ?</p>
            <div class="flex justify-end">
                <button @click="confirmDelete" class="bg-red-500 hover:bg-red-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline mr-2">
                    Supprimer
                </button>
                <button @click="closeDeleteModal" class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline">
                    Annuler
                </button>
            </div>
        </div>
    </div>
</template>
