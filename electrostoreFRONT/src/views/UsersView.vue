<script setup>
import { ref, inject } from "vue";
import router from "@/router";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useUsersStore, useAuthStore } from "@/stores";
const usersStore = useUsersStore();
const authStore = useAuthStore();

import { UserRole } from "@/enums";

if (!authStore.hasPermission([1, 2])) {
	addNotification({ message: "vous n'avez pas la permission d'acceder a cette page", type: "error", i18n: false });
	router.push("/");
}

const userTypeRole = ref({ [UserRole.User]: t("user.VUsersFilterRole0"), [UserRole.Moderator]: t("user.VUsersFilterRole1"), [UserRole.Admin]: t("user.VUsersFilterRole2") });

const filter = ref([
	{ key: "nom_user", value: "", type: "text", label: "user.VUsersFilterName", compareMethod: "contain" },
	{ key: "prenom_user", value: "", type: "text", label: "user.VUsersFilterFirstName", compareMethod: "contain" },
	{ key: "email_user", value: "", type: "text", label: "user.VUsersFilterEmail", compareMethod: "contain" },
	{ key: "role_user", value: "", type: "datalist", typeData: "int", options: userTypeRole, label: "user.VUsersFilterRole", compareMethod: "=" },
]);
const tableauLabel = ref([
	{ label: "user.VUsersName", sortable: true, key: "nom_user", type: "text" },
	{ label: "user.VUsersFirstName", sortable: true, key: "prenom_user", type: "text" },
	{ label: "user.VUsersEmail", sortable: true, key: "email_user", type: "text" },
	{ label: "user.VUsersRole", sortable: true, key: "role_user", type: "enum", options: userTypeRole },
]);
const tableauMeta = ref({
	key: "id_user",
	path: "/users/",
});
document.querySelector("#view").classList.remove("overflow-y-scroll");
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('user.VUsersTitle') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/users/new'">
				{{ $t('user.VUsersAdd') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('user.VUsersAdd') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="usersStore.users" />
	</div>
	<Tableau :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[usersStore.users]"
		:filters="filter"
		:loading="usersStore.usersLoading"
		:total-count="Number(usersStore.usersTotalCount) || 0"
		:fetch-function="(limit, offset, expand, filter, sort, clear) => usersStore.getUserByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
