<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useAuthStore, useStoresStore, useTagsStore } from "@/stores";
const storesStore = useStoresStore();
const tagsStore = useTagsStore();
const authStore = useAuthStore();

async function fetchData() {
	let tagsLink = new Set();
	let offset = 0;
	const limit = 100;
	do {
		await storesStore.getStoreByInterval(limit, offset, ["stores_tags"]);
		offset += limit;
	} while (offset < storesStore.storesTotalCount);
	for (const store in storesStore.stores) {
		for (const tag in storesStore.storeTags[store]) {
			tagsLink.add(tag);
		}
	}
	let tagsNotFound = [];
	for (const tag of Array.from(tagsLink)) {
		if (!tagsStore.tags[tag]) {
			tagsNotFound.push(tag);
		}
	}
	if (tagsNotFound.length > 0) {
		await tagsStore.getTagByList(tagsNotFound);
	}
	filter.value[4].options = Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]);
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
	{ key: "nom_store", value: "", type: "text", label: "store.VStoresFilterName", compareMethod: "contain" },
	{ key: "mqtt_name_store", value: "", type: "text", label: "store.VStoresFilterMqttName", compareMethod: "contain" },
	{ key: "xlength_store", value: "", type: "number", label: "store.VStoresFilterXLength", compareMethod: "<=" },
	{ key: "ylength_store", value: "", type: "number", label: "store.VStoresFilterYLength", compareMethod: "<=" },
	{ key: "id_tag", subPath: "stores_tags", value: "", type: "select", options: Object.values(tagsStore.tags).map((tag) => [tag.id_tag, tag.nom_tag]), label: "store.VStoresFilterTag", compareMethod: "=" },
]);
const filteredStores = computed(() => {
	return Object.values(storesStore.stores).filter((element) => {
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
const sortedStores = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredStores.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredStores.value;
});
</script>

<template>
	<div>
		<h2>stores</h2>
	</div>
	<div>
		<div :disabled="authStore.user?.role_user !== 'admin'"
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/stores/new'">
				Ajouter
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
					@click="changeSort('nom_store')">
					<div class="flex justify-between items-center">
						<span class="flex-1">nom</span>
						<template v-if="sort.key === 'nom_store'">
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
					@click="changeSort('xlength_store')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('store.VStoresXLength') }}</span>
						<template v-if="sort.key === 'xlength_store'">
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
					@click="changeSort('ylength_store')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('store.VStoresYLength') }}</span>
						<template v-if="sort.key === 'ylength_store'">
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
					@click="changeSort('mqtt_name_store')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('store.VStoresMqttName') }}</span>
						<template v-if="sort.key === 'mqtt_name_store'">
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
						<span class="flex-1">{{ $t('store.VStoresTagsList') }}</span>
					</div>
				</th>
			</tr>
		</thead>
		<tbody>
			<template v-if="!storesStore.stores.storesLoading">
				<RouterLink v-for="store in sortedStores" :key="store.id_store" :to="'/stores/' + store.id_store" custom
					v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.nom_store }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.xlength_store }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.ylength_store }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ store.mqtt_name_store }}
						</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">
							<ul>
								<li v-for="tag in storesStore.storeTags[store.id_store]" :key="tag.id_tag">
									{{ tagsStore.tags[tag.id_tag]?.nom_tag }}
								</li>
							</ul>
						</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('store.VStoresLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>
