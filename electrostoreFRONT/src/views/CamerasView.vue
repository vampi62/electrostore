<script setup>
import { onMounted, ref, computed } from 'vue';
import { useCamerasStore } from '@/stores';
const camerasStore = useCamerasStore();

async function fetchData() {
    camerasStore.getCameraByInterval(100, 0);
}
onMounted(() => {
    fetchData();
});

const sort = ref({ key: '', order: 'asc' });
function changeSort(key) {
    if (sort.value.key === key) {
        sort.value.order = sort.value.order === 'asc' ? 'desc' : 'asc';
    } else {
        sort.value.key = key;
        sort.value.order = 'asc';
    }
}
const filter = ref([
    { key: 'nom_camera', value: '', type: 'text', label: 'nom', compareMethod: 'contain' },
    { key: 'url_camera', value: '', type: 'text', label: 'url', compareMethod: 'contain' }
]);
const filteredCameras = computed(() => {
    return Object.values(camerasStore.cameras).filter(element => {
        return filter.value.every(f => {
            if (f.value) {
                if (f.subPath) {
                    if (f.compareMethod === '=') {
                        return element[f.subPath].some(subElement => subElement[f.key] === f.value);
                    } else if (f.compareMethod === '>=') {
                        return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
                    } else if (f.compareMethod === '<=') {
                        return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
                    }
                } else {
                    if (f.compareMethod === '=') {
                        return element[f.key] === f.value;
                    } else if (f.compareMethod === '>=') {
                        return element[f.key] >= f.value;
                    } else if (f.compareMethod === '<=') {
                        return element[f.key] <= f.value;
                    } else if (f.compareMethod === 'contain') {
                        return element[f.key].includes(f.value);
                    }
                }
            }
            return true;
        });
    });
});
const sortedCameras = computed(() => {
    if (sort.value.key) {
        return Object.values(filteredCameras.value).sort((a, b) => {
            if (sort.value.order === 'asc') {
                return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
            } else {
                return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
            }
        });
    }
    return filteredCameras.value;
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
</script>

<template>
    <div>
        <h2>camera</h2>
    </div>
    <div>
        <button class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
            Ajouter
        </button>
        <div>
            <div class="flex flex-wrap">
                <template v-for="f in filter">
                    <label class="text-sm text-gray-700 ml-2">{{ f.label }}</label>
                    <template v-if="f.type === 'select'">
                        <select v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2">
                            <option value=""></option>
                            <template v-if="f.options">
                                <option v-for="option in f.options" :key="option[0]" :value="option[0]">{{ option[1] }}
                                </option>
                            </template>
                        </select>
                    </template>
                    <template v-else-if="f.type === 'date'">
                        <input type="date" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                    <template v-else-if="f.type === 'number'">
                        <input type="number" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                    <template v-else-if="f.type === 'text'">
                        <input type="text" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
                    </template>
                </template>
            </div>
        </div>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('nom_camera')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">nom</span>
                        <template v-if="sort.key === 'nom_camera'">
                            <template v-if="sort.order === 'asc'">
                                <font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
                            </template>
                            <template v-else>
                                <font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
                            </template>
                        </template>
                        <template v-else>
                            <font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
                        </template>
                    </div>
                </th>
                <th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
                    @click="changeSort('url_camera')">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">url</span>
                        <template v-if="sort.key === 'url_camera'">
                            <template v-if="sort.order === 'asc'">
                                <font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
                            </template>
                            <template v-else>
                                <font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
                            </template>
                        </template>
                        <template v-else>
                            <font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
                        </template>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">login</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">password</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1">capture</span>
                    </div>
                </th>
                <th
                    class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
                    <div class="flex justify-between items-center">
                        <span class="flex-1"></span>
                    </div>
                </th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!camerasStore.loading">
                <RouterLink v-for="camera in sortedCameras" :key="camera.id_camera"
                    :to="'/cameras/' + camera.id_camera" custom v-slot="{ navigate }">
                    <tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.nom_camera }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.url_camera }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.user_camera }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.mdp_camera }}</td>
                        <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                            <button @click="camerasStore.getCapture(camera.id_camera)"
                                class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm">
                                capture
                            </button>
                            <div v-if="camerasStore.capture[camera.id_camera]">
                                <img alt="Image provenant d'un flux vidéo" :src="camerasStore.capture[camera.id_camera]"
                                    class="w-16 h-16 object-cover rounded" />
                            </div>
                        </td>
                        <td class="border border-gray-300 px-4 py-2 text-center text-sm text-gray-700">
                            <button @click="openDeleteModal(camera)"
                                class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm">
                                Supprimer
                            </button>
                        </td>
                    </tr>
                </RouterLink>
            </template>
        </tbody>
    </table>

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
