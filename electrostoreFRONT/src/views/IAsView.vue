<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useAuthStore, useIasStore } from "@/stores";
const IAStore = useIasStore();
const authStore = useAuthStore();

async function fetchData() {
	let offset = 0;
	const limit = 100;
	do {
		await IAStore.getIaByInterval(limit, offset);
		offset += limit;
	} while (offset < IAStore.TotalCount);
}
onMounted(() => {
	fetchData();
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
	{ key: "trained_ia", value: "", type: "select", options: [[false, t("ia.VIasFilterTrained1")], [true, t("ia.VIasFilterTrained2")]], label: "ia.VIasFilterTrained", compareMethod: "=" },
	{ key: "date_ia", value: "", type: "date", label: "ia.VIasFilterDate", compareMethod: ">=" },
	{ key: "nom_ia", value: "", type: "text", label: "ia.VIasFilterNom", compareMethod: "contain" },
]);
const filteredIas = computed(() => {
	return Object.values(IAStore.ias).filter((element) => {
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
const sortedIas = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredIas.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredIas.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('ia.VIasTitle') }}</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 'admin'"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/ia/new'">
				{{ $t('ia.VIasAdd') }}
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
					@click="changeSort('nom_ia')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('ia.VIasName') }}</span>
						<template v-if="sort.key === 'nom_ia'">
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
				<th
					class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 relative">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('ia.VIasDescription') }}</span>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('date_ia')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('ia.VIasDate') }}</span>
						<template v-if="sort.key === 'date_ia'">
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
					@click="changeSort('trained_ia')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('ia.VIasTrained') }}</span>
						<template v-if="sort.key === 'trained_ia'">
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
			<template v-if="!IAStore.loading">
				<RouterLink v-for="ia in sortedIas" :key="ia.id_ia" :to="'/ia/' + ia.id_ia" custom
					v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.nom_ia }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.description_ia }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ ia.date_ia }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">
							<template v-if="ia.trained_ia">
								<font-awesome-icon icon="fa-solid fa-check" class="text-green-500" />
							</template>
							<template v-else>
								<font-awesome-icon icon="fa-solid fa-times" class="text-red-500" />
							</template>
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('ia.VIasLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>
