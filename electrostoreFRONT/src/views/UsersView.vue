<script setup>
import { onMounted, ref, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useUsersStore } from "@/stores";
const usersStore = useUsersStore();
const authStore = useAuthStore();

if (authStore.user?.role_user !== 2 && authStore.user?.role_user !== 1) {
	addNotification({ message: "vous n'avez pas la permission d'acceder a cette page", type: "error", i18n: false });
	router.push("/");
}

async function fetchAllData() {
	let offset = 0;
	const limit = 100;
	do {
		await usersStore.getUserByInterval(limit, offset);
		offset += limit;
	} while (offset < usersStore.usersTotalCount);
}
onMounted(() => {
	fetchAllData();
});

const filter = ref([
	{ key: "nom_user", value: "", type: "text", label: "user.VUsersFilterName", compareMethod: "contain" },
	{ key: "prenom_user", value: "", type: "text", label: "user.VUsersFilterFirstName", compareMethod: "contain" },
	{ key: "email_user", value: "", type: "text", label: "user.VUsersFilterEmail", compareMethod: "contain" },
	{ key: "role_user", value: "", type: "select", typeData: "int", options: [[0, t("user.VUsersFilterRole0")], [1, t("user.VUsersFilterRole1")], [2, t("user.VUsersFilterRole2")]], label: "user.VUsersFilterRole", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "user.VUsersName", sortable: true, key: "nom_user", type: "text" },
	{ label: "user.VUsersFirstName", sortable: true, key: "prenom_user", type: "text" },
	{ label: "user.VUsersEmail", sortable: true, key: "email_user", type: "text" },
	{ label: "user.VUsersRole", sortable: true, key: "role_user", type: "enum", options: [t("user.VUsersFilterRole0"), t("user.VUsersFilterRole1"), t("user.VUsersFilterRole2")] },
]);
const tableauMeta = ref({
	key: "id_user",
	path: "/users/",
});
const filteredUsers = ref([]);
const updateFilteredUsers = (newValue) => {
	filteredUsers.value = newValue;
};
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('user.VUsersTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 2"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/users/new'">
				{{ $t('user.VUsersAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="usersStore.users" @output-filter="updateFilteredUsers" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[filteredUsers]"
		:loading="usersStore.usersLoading"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
