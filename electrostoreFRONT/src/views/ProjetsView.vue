<script setup>

import { useProjetsStore } from '@/stores';
const projetsStore = useProjetsStore();
projetsStore.getProjetByInterval();
</script>

<template>
    <div>
        <h2>liste des projets</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">description
                </th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">url</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">status</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">date</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">date_fin</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!projetsStore.projets.projetsLoading">
                <tr v-for="projet in projetsStore.projets" :key="projet.id_projet">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.id_projet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.nom_projet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.description_projet }}
                    </td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.url_projet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.status_projet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.date_projet }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ projet.date_fin_projet }}</td>
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
