<script setup>
import { onMounted, ref, computed, inject } from 'vue';


const { addNotification } = inject('useNotification');




import { useI18n } from 'vue-i18n';
const { t } = useI18n();

import { RouterView, RouterLink, useRoute } from 'vue-router';
const route = useRoute();


import { useAuthStore, useConfigsStore } from '@/stores';

const configsStore = useConfigsStore();
const authStore = useAuthStore();

configsStore.getConfig();

const isIframe = computed(() => route.query.iframe != null);


const reduceLeftSideBar = ref(false);
const showTopBar = ref(false);
const listNav = [
  { name: t('VAppInventory'), path: '/inventory', roleRequired: 'user', faIcon: 'fa-solid fa-box' },
  { name: t('VAppProjet'), path: '/projets', roleRequired: 'user', faIcon: 'fa-solid fa-project-diagram' },
  { name: t('VAppCommand'), path: '/commands', roleRequired: 'user', faIcon: 'fa-solid fa-shopping-cart' },
  { name: t('VAppCam'), path: '/cameras', roleRequired: 'user', faIcon: 'fa-solid fa-camera' },
  { name: t('VAppIa'), path: '/ia', roleRequired: 'user', faIcon: 'fa-solid fa-microchip' },
  { name: t('VAppTags'), path: '/tags', roleRequired: 'user', faIcon: 'fa-solid fa-tags' },
  { name: t('VAppStores'), path: '/stores', roleRequired: 'user', faIcon: 'fa-solid fa-store' }
];

const containerClasses = computed(() => [
  'p-4 overflow-y-scroll fixed bottom-0 right-0 left-0',
  reduceLeftSideBar.value && authStore.user && !isIframe.value ? 'sm:ml-16' : '',
  !reduceLeftSideBar.value && authStore.user && !isIframe.value ? 'sm:ml-64' : '',
  authStore.user ? 'top-16' : 'top-0'
]);
</script>

<template>
  <div class="app-container">
    <div v-show="authStore.user && !isIframe">
      <nav class="flex justify-between p-5 bg-gray-800 border-b-2 border-blue-400 fixed w-full top-0 h-16">
        <RouterLink to="/" class="text-white hover:text-blue-400">{{ $t('VAppHome') }}</RouterLink>
        <a href="https://github.com/vampi62/electrostore" class="block sm:hidden text-white hover:text-blue-400"
          target="_blank" rel="noopener noreferrer"><!-- for mobile -->
          <p class="space-x-4">
            <font-awesome-icon icon="fa-brands fa-github" size="lg" />
            <span>ElectroStore</span>
          </p>
        </a>
        <button @click="showTopBar = !showTopBar"
          class="block sm:hidden text-white hover:text-blue-400"><!-- for mobile -->
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16m-7 6h7">
            </path>
          </svg>
        </button>
        <div class="hidden sm:flex"><!-- for desktop -->
          <div class="flex space-x-4 justify-end">
            <RouterLink to="/profile" class="text-white hover:text-blue-400">{{ $t('VAppProfile') }}</RouterLink>
            <RouterLink v-if="authStore.user?.role_user === 'admin'" to="/users" class="text-white hover:text-blue-400">
              {{ $t('VAppAdmin') }}</RouterLink>
            <a v-if="authStore.user" @click="authStore.logout()"
              class="cursor-pointer text-white hover:text-blue-400">{{ $t('VAppLogout') }}</a>
          </div>
        </div>
      </nav>
      <div class="fixed sm:hidden top-12 w-full z-50"><!-- for mobile -->
        <div v-show="showTopBar" class="flex flex-col space-y-4 bg-gray-800 p-4">
          <RouterLink to="/profile" class="text-white hover:text-blue-400">{{ $t('VAppProfile') }}</RouterLink>
          <RouterLink v-if="authStore.user?.role_user === 'admin'" to="/users" class="text-white hover:text-blue-400">
            {{ $t('VAppAdmin') }}</RouterLink>
          <a v-if="authStore.user" @click="authStore.logout()"
            class="cursor-pointer text-white hover:text-blue-400">{{ $t('VAppLogout') }}</a>
          <div class="border-t-2 border-blue-400"></div>
          <ul class="mt-6 space-y-4">
            <li v-for="nav in listNav" :key="nav.name">
              <RouterLink :to="nav.path" :class="['flex items-center space-x-4 hover:text-blue-400',
                route.path.includes(nav.path) ? 'text-blue-400' : 'text-white']">
                <font-awesome-icon :icon="nav.faIcon" />
                <span>{{ nav.name }}</span>
              </RouterLink>
            </li>
          </ul>
        </div>
      </div>
      <div :class="['hidden sm:flex flex-col justify-between p-4 bg-gray-800 fixed left-0 top-16 bottom-12',
        reduceLeftSideBar ? 'w-16' : 'w-64']"><!-- for desktop -->
        <div class="flex flex-col space-y-4">
          <ul class="mt-2 space-y-4">
            <li v-for="nav in listNav" :key="nav.name" class="min-h-6">
              <RouterLink :to="nav.path" :class="['flex items-center space-x-4 hover:text-blue-400',
                route.path.includes(nav.path) ? 'text-blue-400' : 'text-white']">
                <div class="flex items-center justify-center w-8 h-8">
                  <font-awesome-icon :icon="nav.faIcon" size="lg" />
                </div>
                <span v-if="!reduceLeftSideBar" class="whitespace-nowrap">{{ nav.name }}</span>
              </RouterLink>
            </li>
          </ul>
        </div>
        <a href="https://github.com/vampi62/electrostore" class="block text-white hover:text-blue-400" target="_blank"
          rel="noopener noreferrer">
          <div class="text-center mt-4">
            <p class="space-x-4">
              <font-awesome-icon icon="fa-brands fa-github" size="lg" />
              <span v-if="!reduceLeftSideBar">ElectroStore</span>
            </p>
          </div>
        </a>
      </div>
      <button :class="['hidden sm:flex justify-center p-4 bg-gray-700 text-white hover:text-blue-400 fixed left-0 bottom-0 h-12',
        reduceLeftSideBar ? 'w-16' : 'w-64']" @click="reduceLeftSideBar = !reduceLeftSideBar">
        <font-awesome-icon v-if="reduceLeftSideBar" icon="fa-solid fa-arrow-right" size="lg" />
        <font-awesome-icon v-else icon="fa-solid fa-arrow-left" size="lg" />
      </button>
    </div>
    <div :class="containerClasses">
      <RouterView />
    </div>
    <NotificationContainer />
  </div>
</template>