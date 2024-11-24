<script setup>

import { useCommandesStore } from '@/stores';
const commandsStore = useCommandesStore();
commandsStore.getAll();
</script>

<template>
    <div>
        <h2>commandes liste</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">prix</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">url</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">status</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">date</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">date_livraison</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <tr v-if="!commandsStore.commandes.loading" v-for="command in commandsStore.commandes" :key="command.id_command">
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.id_command }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.prix_command }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.url_command }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.status_command }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.date_command }}</td>
                <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ command.date_livraison_command }}</td>
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
