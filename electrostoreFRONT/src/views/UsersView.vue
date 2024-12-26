<script setup>
import { storeToRefs } from 'pinia';

import { useUsersStore } from '@/stores';

const usersStore = useUsersStore();
const { users: users } = storeToRefs(usersStore);

usersStore.getUserByInterval();
</script>

<template>
    <div>
        <h2>liste des utilisateurs</h2>
    </div>
    <table class="min-w-full border-collapse border border-gray-300">
        <thead class="bg-gray-100">
            <tr>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">id</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">nom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">prenom</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">email</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">role</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">edit</th>
                <th class="border border-gray-300 px-4 py-2 text-left text-sm font-medium text-gray-700">supprimer</th>
            </tr>
        </thead>
        <tbody>
            <template v-if="!users.usersLoading">
                <tr v-for="user in users" :key="user.id_user">
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ user.id_user }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ user.nom_user }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ user.prenom_user }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ user.email_user }}</td>
                    <td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ user.role_user }}</td>
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
    <div v-if="users.usersLoading" class="spinner-border spinner-border-sm"></div>
    <div v-if="users.error" class="text-danger">Error loading users: {{ users.error }}</div>
</template>
