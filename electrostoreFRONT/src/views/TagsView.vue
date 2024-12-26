<script setup>

import { useTagsStore } from '@/stores';
const tagsStore = useTagsStore();
tagsStore.getTagByInterval();
</script>

<template>
    <div>
        <h2>Tags</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">poids_tag</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!tagsStore.tagsLoading">
                <tr v-for="tag in tagsStore.tags" :key="tag.id_tag">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ tag.id_tag }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ tag.nom_tag }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ tag.poids_tag }}</td>
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
