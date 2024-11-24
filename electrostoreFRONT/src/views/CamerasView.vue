<script setup>
import { useCamerasStore } from '@/stores';
const camerasStore = useCamerasStore();
camerasStore.getAll();
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
            </tr>
        </thead>
        <tbody>
            <tr v-if="!camerasStore.cameras.loading" v-for="camera in camerasStore.cameras" :key="camera.id_camera">
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.id_camera }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.nom_camera }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.url_camera }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.user_camera }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ camera.mdp_camera }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                    <button class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
                        Modifier
                    </button>
                </td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
                    <button class="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm ml-2">
                        Supprimer
                    </button>
                </td>
            </tr>
        </tbody>
    </table>
</template>
