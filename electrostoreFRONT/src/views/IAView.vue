<script setup>

import { useIasStore } from '@/stores';
const IAStore = useIasStore();
IAStore.getAll();
</script>

<template>
    <div>
        <h2>IA</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">description</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">date</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">trained</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!IAStore.ias.loading">
                <tr v-for="ia in IAStore.ias" :key="ia.id_ia">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.id_ia }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.nom_ia }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.description_ia }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.date_ia }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.trained_ia }}</td>
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
