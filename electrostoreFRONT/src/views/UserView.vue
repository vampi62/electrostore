<script setup>
import { onMounted, onBeforeUnmount, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { Form, Field } from "vee-validate";
import * as Yup from "yup";

import { useRoute } from "vue-router";
const route = useRoute();
const userId = route.params.id;

import { useUsersStore, useCommandsStore, useProjetsStore, useAuthStore } from "@/stores";
const usersStore = useUsersStore();
const commandsStore = useCommandsStore();
const projetsStore = useProjetsStore();
const authStore = useAuthStore();

async function fetchData() {
	if (userId !== "new") {
		usersStore.userEdition = {
			loading: false,
		};
		try {
			await usersStore.getUserById(userId);
		} catch {
			delete usersStore.users[userId];
			addNotification({ message: "command.VCommandUpdated", type: "error", i18n: true });
			router.push("/users");
			return;
		}
		usersStore.getProjectCommentaireByInterval(userId, 100, 0, ["projet"]);
		usersStore.getCommandCommentaireByInterval(userId, 100, 0, ["command"]);
		usersStore.getTokenByInterval(userId, 100, 0);
		usersStore.userEdition = {
			loading: false,
			nom_user: usersStore.users[userId].nom_user,
			prenom_user: usersStore.users[userId].prenom_user,
			email_user: usersStore.users[userId].email_user,
			role_user: usersStore.users[userId].role_user,
		};
	} else {
		usersStore.userEdition = {
			loading: false,
		};
	}
}
onMounted(() => {
	fetchData();
});
onBeforeUnmount(() => {
	usersStore.userEdition = {
		loading: false,
	};
});
</script>

<template>
	<div class="flex items-center justify-between mb-4">
		<h2 class="text-2xl font-bold mb-4">{{ $t('user.VUserTitle') }}</h2>
		<div class="flex space-x-4">
			<button type="button" @click="userSave" v-if="userId == 'new'"
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="usersStore.userEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('user.VUserAdd') }}</button>
			<button type="button" @click="userUpdate" v-else
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 flex items-center">
				<span v-show="usersStore.userEdition.loading"
					class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2 inline-block"></span>{{
						$t('user.VUserUpdate') }}</button>
			<button type="button" @click="userDeleteOpenModal" v-if="userId != 'new'"
				class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600">{{ $t('user.VUserDelete') }}</button>
			<RouterLink to="/users"
				class="bg-gray-300 text-gray-800 hover:bg-gray-400 px-4 py-2 rounded flex items-center">{{
					$t('user.VUserBack') }}</RouterLink>
		</div>
	</div>
</template>
