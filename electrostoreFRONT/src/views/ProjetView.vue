<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const projetId = route.params.id;

import { useProjetsStore, useUsersStore, useItemsStore, useAuthStore } from "@/stores";
const projetsStore = useProjetsStore();
const usersStore = useUsersStore();
const itemsStore = useItemsStore();
const authStore = useAuthStore();

async function fetchData() {
	if (projetId !== "new") {
		projetsStore.projetEdition = {
			loading: false,
		};
		try {
			await projetsStore.getProjetById(projetId);
		} catch {
			delete projetsStore.projets[projetId];
			addNotification({ message: "projet.VProjetNotFound", type: "error", i18n: true });
			router.push("/projets");
			return;
		}
		projetsStore.projetEdition = {
			loading: false,
			nom_projet: projetsStore.projets[projetId].nom_projet,
			description_projet: projetsStore.projets[projetId].description_projet,
			projet_url: projetsStore.projets[projetId].projet_url,
			projet_port: projetsStore.projets[projetId].projet_port,
			projet_user: projetsStore.projets[projetId].projet_user,
			projet_mdp: projetsStore.projets[projetId].projet_mdp,
		};
	} else {
		projetsStore.projetEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	projetsStore.projetEdition = {
		loading: false,
	};
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('projet.VProjetTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="projetSave" v-if="projetId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="projetsStore.projetEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('projet.VProjetAdd') }}
			</button>
			<button type="button" @click="projetUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="projetsStore.projetEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block">
				</span>
				{{ $t('projet.VProjetUpdate') }}
			</button>
			<button type="button" @click="projetDeleteOpenModal" v-if="projetId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">
				{{ $t('projet.VProjetDelete') }}
			</button>
			<RouterLink to="/projets"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">
				{{ $t('projet.VProjetBack') }}
			</RouterLink>
		</div>
	</div>
</template>
