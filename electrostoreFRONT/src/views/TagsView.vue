<script setup>
import { onMounted, ref, computed, inject } from "vue";

const { addNotification } = inject("useNotification");

import { useTagsStore } from "@/stores";
const tagsStore = useTagsStore();

async function fetchData() {
	let offset = 0;
	const limit = 100;
	do {
		await tagsStore.getTagByInterval(limit, offset);
		offset += limit;
	} while (offset < tagsStore.tagsTotalCount);
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
	{ key: "nom_tag", value: "", type: "text", label: "tag.VTagsFilterName", compareMethod: "contain" },
	{ key: "poids_tag", value: "", type: "number", label: "tag.VTagsFilterWeightMin", compareMethod: ">=" },
	{ key: "poids_tag", value: "", type: "number", label: "tag.VTagsFilterWeightMax", compareMethod: "<=" },
]);
const filteredTags = computed(() => {
	return Object.values(tagsStore.tags).filter((element) => {
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
const sortedTags = computed(() => {
	if (sort.value.key) {
		return Object.values(filteredTags.value).sort((a, b) => {
			if (sort.value.order === "asc") {
				return a[sort.value.key] > b[sort.value.key] ? 1 : -1;
			} else {
				return a[sort.value.key] < b[sort.value.key] ? 1 : -1;
			}
		});
	}
	return filteredTags.value;
});
</script>

<template>
	<div>
		<h2 class="text-2xl font-bold mb-4">{{ $t('tag.VTagsTitle') }}</h2>
	</div>
	<div>
		<div
			class="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm cursor-pointer inline-block mb-2">
			<RouterLink :to="'/tags/new'">
				{{ $t('tag.VTagsAdd') }}
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
					@click="changeSort('nom_tag')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('tag.VTagsName') }}</span>
						<template v-if="sort.key === 'nom_tag'">
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
					@click="changeSort('poids_tag')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('tag.VTagsWeight') }}</span>
						<template v-if="sort.key === 'poids_tag'">
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
					@click="changeSort('items_tags_count')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('tag.VTagsItemsCount') }}</span>
						<template v-if="sort.key === 'items_tags_count'">
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
					@click="changeSort('stores_tags_count')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('tag.VTagsStoresCount') }}</span>
						<template v-if="sort.key === 'stores_tags_count'">
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
					@click="changeSort('boxs_tags_count')">
					<div class="flex justify-between items-center">
						<span class="flex-1">{{ $t('tag.VTagsBoxsCount') }}</span>
						<template v-if="sort.key === 'boxs_tags_count'">
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
			<template v-if="!tagsStore.tagsLoading">
				<RouterLink v-for="tag in sortedTags" :key="tag.id_tag" :to="'/tags/' + tag.id_tag" custom
					v-slot="{ navigate }">
					<tr @click="navigate" class=" transition duration-150 ease-in-out hover:bg-gray-200 cursor-pointer">
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ tag.nom_tag }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700">{{ tag.poids_tag }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">{{
							tag.items_tags_count }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">{{
							tag.stores_tags_count }}</td>
						<td class="border border-gray-300 px-4 py-2 text-sm text-gray-700 text-center">{{
							tag.boxs_tags_count }}</td>
					</tr>
				</RouterLink>
			</template>
			<template v-else>
				<div>{{ $t('tag.VTagsLoading') }}</div>
			</template>
		</tbody>
	</table>
</template>
