<script setup lang="ts">
import { ref, inject } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import { useViewScroll } from "@/composables";
import type { useNotification } from "@/composables";
import { UserRole } from "@/enums";
import { useUsersStore, useAuthStore } from "@/stores";

import type { FilterLabel } from "@/types/filter";
import type { TableauLabel, TableauMeta } from "@/types/tableau";
import type { RSQLFilter, RSQLSort } from "@/types/rsql";

const { addNotification } = inject("useNotification") as ReturnType<typeof useNotification>;
const { t } = useI18n();
const router = useRouter();

const usersStore = useUsersStore();
const authStore = useAuthStore();

if (!authStore.hasPermission([1, 2])) {
	addNotification({ message: t("users.noAccess"), type: "error" });
	router.push("/");
}

const userTypeRole = ref({ [UserRole.User]: t("users.FilterRole0"), [UserRole.Moderator]: t("users.FilterRole1"), [UserRole.Admin]: t("users.FilterRole2") });

const filter = ref<FilterLabel[]>([
	{ key: "name_user", value: "", type: "text", label: "users.FilterName", compareMethod: "=like=" },
	{ key: "firstname_user", value: "", type: "text", label: "users.FilterFirstName", compareMethod: "=like=" },
	{ key: "email_user", value: "", type: "text", label: "users.FilterEmail", compareMethod: "=like=" },
	{ key: "role_user", value: "", type: "datalist", typeData: "number", options: userTypeRole, sortOptions: "asc", label: "users.FilterRole", compareMethod: "==" },
]);
const tableauLabel = ref<TableauLabel[]>([
	{ label: "users.Name", sortable: true, key: "name_user", valueKey: "name_user", type: "text" },
	{ label: "users.FirstName", sortable: true, key: "firstname_user", valueKey: "firstname_user", type: "text" },
	{ label: "users.Email", sortable: true, key: "email_user", valueKey: "email_user", type: "text" },
	{ label: "users.Role", sortable: true, key: "role_user", valueKey: "role_user", type: "enum", options: userTypeRole },
]);
const tableauMeta = ref<TableauMeta>({
	key: "id_user",
	path: "/users/",
	saveState: true,
	stateKey: "usersTableState",
});
const filterReady = ref(false);
useViewScroll(false);
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4 mr-2">{{ $t('users.Title') }}</h2>
	</div>
	<div>
		<div :class="{
				'bg-blue-500 hover:bg-blue-600 cursor-pointer': authStore.hasPermission([2]),
				'bg-gray-400 cursor-not-allowed': !authStore.hasPermission([2])
			}"
			class="text-white px-4 py-2 rounded inline-block mb-2">
			<RouterLink v-if="authStore.hasPermission([2])" :to="'/users/new'">
				{{ $t('users.Add') }}
			</RouterLink>
			<span v-else class="pointer-events-none">
				{{ $t('users.Add') }}
			</span>
		</div>
		<FilterContainer :filters="filter" :store-data="usersStore.users" @ready="filterReady = true" :save-state="true" state-key="usersFilterState" />
	</div>
	<Tableau v-if="filterReady" :labels="tableauLabel" :meta="tableauMeta"
		:store-data="[usersStore.users]"
		:filters="filter"
		:loading="usersStore.usersLoading"
		:total-count="Number(usersStore.usersTotalCount) || 0"
		:fetch-function="(limit: number, offset: number, expand: string[], filter: RSQLFilter[], sort: RSQLSort, clear: boolean) => usersStore.getUserByInterval(limit, offset, expand, filter, sort, clear)"
		:tableau-css="{ component: 'flex-1 overflow-y-auto'}"
	/>
</template>
