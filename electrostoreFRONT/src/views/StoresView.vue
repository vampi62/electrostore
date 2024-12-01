<script setup>

import { useStoresStore } from '@/stores';
const storesStore = useStoresStore();
storesStore.getAll();
</script>

<template>
    <div>
        <h2>stores</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">xlength</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">ylength</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">mqtt_name</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!storesStore.stores.loading">
                <tr v-for="store in storesStore.stores" :key="store.id_store">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.id_store }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.nom_store }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.xlength_store }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.ylength_store }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.mqtt_name_store }}</td>
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
            </template>
        </tbody>
    </table>
</template>
