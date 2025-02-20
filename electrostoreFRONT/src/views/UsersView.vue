<script setup>
import { onMounted, ref, computed, inject } from "vue";
import { router } from "@/helpers";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useUsersStore } from "@/stores";
const usersStore = useUsersStore();
const authStore = useAuthStore();

if (authStore.user?.role_user !== "admin") {
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

const sort = ref({ key: "", order: "asc" });
function changeSort(key) {
	if (sort.value.key === key) {
		sort.value.order = sort.value.order === "asc" ? "desc" : "asc";
	} else {
		sort.value.key = key;
		sort.value.order = "asc";
	}
}
const filter = ref([
	{ key: "nom_user", value: "", type: "text", label: "user.VUsersFilterName", compareMethod: "contain" },
	{ key: "prenom_user", value: "", type: "text", label: "user.VUsersFilterFirstName", compareMethod: "contain" },
	{ key: "email_user", value: "", type: "text", label: "user.VUsersFilterEmail", compareMethod: "contain" },
	{ key: "role_user", value: "", type: "select", options: [["admin", t("user.VUsersFilterRole1")], ["user", t("user.VUsersFilterRole2")]], label: "user.VUsersFilterRole", compareMethod: "=" },
]);
const filteredUsers = computed(() => {
	return Object.values(usersStore.users).filter((element) => {
		return filter.value.every((f) => {
			if (f.value) {
				if (f.subPath) {
					if (f.compareMethod === "=") {
						return element[f.subPath].some((subElement) => subElement[f.key] === f.value);
					} else if (f.compareMethod === ">=") {
						return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) >= f.value;
					} else if (f.compareMethod === "<=") {
						return element[f.subPath].reduce((total, subElement) => total + subElement[f.key], 0) <= f.value;
					}
				} else {
					if (f.compareMethod === "=") {
						return element[f.key] === f.value;
					} else if (f.compareMethod === ">=") {
						return element[f.key] >= f.value;
					} else if (f.compareMethod === "<=") {
						return element[f.key] <= f.value;
					} else if (f.compareMethod === "contain") {
						return element[f.key].includes(f.value);
					}
				}
			}
			return true;
		});
	});
});
const sortedUsers = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredUsers.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredUsers.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('user.VUsersTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 'admin'"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/users/new'">
				{{ $t('user.VUsersAdd') }}
			</RouterLink>
		</div>
		<div>
			<div class="flex flex-wrap">
				<template v-for="f in filter" :key="f">
					<label class="text-sm text-gray-700 ml-2">{{ $t(f.label) }}</label>
					<template v-if="f.type === 'select'">
						<select v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2">
							<option value=""></option>
							<template v-if="f.options">
								<option v-for="option in f.options" :key="option[0]" :value="option[0]">{{ option[1] }}
								</option>
							</template>
						</select>
					</template>
					<template v-else-if="f.type === 'date'">
						<input type="date" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
					<template v-else-if="f.type === 'number'">
						<input type="number" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
					<template v-else-if="f.type === 'text'">
						<input type="text" v-model="f.value" class="border border-gray-300 rounded px-2 py-1 ml-2" />
					</template>
				</template>
			</div>
		</div>
	</div>
	<table class="min-w-full border-collapse border border-gray-300">
		<thead class="bg-gray-100">
			<tr>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('nom_user')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('user.VUsersName') }}</span>
						<template v-if="sort.key === 'nom_user'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('prenom_user')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('user.VUsersFirstName') }}</span>
						<template v-if="sort.key === 'prenom_user'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('email_user')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('user.VUsersEmail') }}</span>
						<template v-if="sort.key === 'email_user'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('role_user')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('user.VUsersRole') }}</span>
						<template v-if="sort.key === 'role_user'">
							<template v-if="sort.order === 'asc'">
								<font-awesome-icon icon="fa-solid fa-sort-up" class="ml-2" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-sort-down" class="ml-2" />
							</template>
						</template>
						<template v-else>
							<font-awesome-icon icon="fa-solid fa-sort" class="ml-2" />
						</template>
					</div>
				</th>
			</tr>
		</thead>
		<tbody>
			<template v-if="!usersStore.usersLoading">
				<RouterLink v-for="user in sortedUsers" :key="user.id_user" :to="'/users/' + user.id_user" custom
					v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ user.nom_user }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ user.prenom_user }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ user.email_user }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ user.role_user }}
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('user.VUsersLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>
