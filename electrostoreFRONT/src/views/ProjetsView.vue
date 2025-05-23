<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useI18n } from "vue-i18n";
const { t } = useI18n();

import { useProjetsStore, useItemsStore } from "@/stores";
const projetsStore = useProjetsStore();
const itemsStore = useItemsStore();

async function fetchAllData() {
	let itemsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await projetsStore.getProjetByInterval(limit, offset, ["projets_items"]);
		offset += limit;
	} while (offset < projetsStore.projetsTotalCount);
	for (const projet in projetsStore.projets) {
		for (const item in projetsStore.items[projet]) {
			itemsLink.add(item);
		}
	}
	let itemsNotFound = [];
	for (const item of Array.from(itemsLink)) {
		if (!itemsStore.items[item]) {
			itemsNotFound.push(item);
		}
	}
	if (itemsNotFound.length > 0) {
		await itemsStore.getItemByList(itemsNotFound);
	}
	filter.value[5].options = Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]);
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
	{ key: "status_projet", value: "", type: "select", options: [["en attente", t("projet.VProjetsFilterStatus1")], ["en cours", t("projet.VProjetsFilterStatus2")], ["terminée", t("projet.VProjetsFilterStatus3")]], label: "projet.VProjetsFilterStatus", compareMethod: "=" },
	{ key: "date_debut_projet", value: "", type: "date", label: "projet.VprojetFilterDate", compareMethod: ">=" },
	{ key: "nom_projet", value: "", type: "text", label: "projet.VprojetFilterNom", compareMethod: "contain" },
	{ key: "url_projet", value: "", type: "text", label: "projet.VprojetFilterUrl", compareMethod: "contain" },
	{ key: "date_fin_projet", value: "", type: "date", label: "projet.VprojetFilterDateEnd", compareMethod: ">=" },
	{ key: "id_item", subPath: "projets_items", value: "", type: "select", options: Object.values(itemsStore.items).map((item) => [item.id_item, item.reference_name_item]), label: "projet.VprojetFilterItem", compareMethod: "=" },
]);
const filteredProjets = ref([]);
const updateFilteredProjets = (newValue) => {
	filteredProjets.value = newValue;
};
const sortedProjets = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredProjets.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredProjets.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ t('projet.VProjetsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/projets/new'">
				{{ t('projet.VProjetsAdd') }}
			</RouterLink>
		</div>
		<FilterContainer :filters="filter" :store-data="projetsStore.projets" @output-filter="updateFilteredProjets" />
	</div>
	<table class="min-w-full border-collapse border border-gray-300">
		<thead class="bg-gray-100">
			<tr>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('nom_projet')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ t('projet.VProjetsName') }}</span>
						<template v-if="sort.key === 'nom_projet'">
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
						<span class="flex-1">{{ t('projet.VProjetsDescription') }}</span>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('url_projet')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ t('projet.VProjetsUrl') }}</span>
						<template v-if="sort.key === 'url_projet'">
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
					@click="changeSort('status_projet')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ t('projet.VProjetsStatus') }}</span>
						<template v-if="sort.key === 'status_projet'">
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
					@click="changeSort('date_debut_projet')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ t('projet.VProjetsDateStart') }}</span>
						<template v-if="sort.key === 'date_debut_projet'">
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
						<span class="flex-1">{{ t('projet.VProjetsItems') }}</span>
					</div>
				</th>
				<th class="border border-gray-300 px-2 py-2 text-center text-sm font-medium text-gray-800 bg-gray-200 cursor-pointer relative"
					@click="changeSort('date_fin_projet')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ t('projet.VProjetsDateEnd') }}</span>
						<template v-if="sort.key === 'date_fin_projet'">
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
			<template v-if="!projetsStore.projets.projetsLoading">
				<RouterLink v-for="projet in sortedProjets" :key="projet.id_projet" :to="'/projets/' + projet.id_projet"
					custom v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.nom_projet }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.description_projet }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.url_projet }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.status_projet }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.date_debut_projet }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							<ul>
								<li v-for="item in projetsStore.items[projet.id_projet]" :key="item.id_item">
									{{ item.qte_projet_item }} - {{ itemsStore.items[item.id_item]?.reference_name_item }}
								</li>
							</ul>
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							{{ projet.date_fin_projet }}
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ t('projet.VProjetsLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>
