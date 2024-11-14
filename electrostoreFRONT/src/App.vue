<script setup>
import { RouterLink, RouterView } from 'vue-router';
import { useAuthStore, useConfigsStore } from '@/stores';

const authStore = useAuthStore();
const configsStore = useConfigsStore();
configsStore.getConfig();
</script>

<template>
    <div class="app-container bg-gray-100 min-h-screen">
        <nav v-show="authStore.user" class="bg-gray-800 text-white p-4">
            <div class="flex space-x-4">
                <RouterLink to="/" class="text-white hover:text-blue-400">Home</RouterLink>
                <a @click="authStore.logout()" class="cursor-pointer text-white hover:text-blue-400">Logout</a>
            </div>
        </nav>
        <div class="container mx-auto py-4">
            <RouterView />
        </div>
        <!-- credits -->
        <div class="text-center mt-4">
            <p>
                <a href="https://github.com/vampi62/electrostore" class="text-blue-500 hover:underline">ElectroStore</a>
            </p>
        </div>
    </div>
</template>